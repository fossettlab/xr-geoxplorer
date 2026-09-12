// Qualification harness only; not the GeoX.Spatial production evaluator.
#include <proj.h>
#include <proj/internal/include_nlohmann_json.hpp>
#include <algorithm>
#include <cmath>
#include <fstream>
#include <limits>
#include <memory>
#include <stdexcept>
#include <string>
using Json = nlohmann::json;
using Object = std::unique_ptr<PJ, decltype(&proj_destroy)>;
using Context = std::unique_ptr<PJ_CONTEXT, decltype(&proj_context_destroy)>;

static Object object(PJ* value) { return Object(value, proj_destroy); }
static std::string text(const char* value) { return value ? value : ""; }
static void require(bool condition, const char* error) {
    if (!condition) throw std::runtime_error(error);
}
static Context context(const std::string& directory) {
    Context ctx(proj_context_create(), proj_context_destroy);
    require(bool(ctx), "context_unavailable");
    proj_log_level(ctx.get(), PJ_LOG_NONE);
    const char* paths[] = {directory.c_str()};
    proj_context_set_search_paths(ctx.get(), 1, paths);
    proj_context_set_enable_network(ctx.get(), 0);
    std::string database = directory + "/proj.db";
    require(proj_context_set_database_path(ctx.get(), database.c_str(), nullptr, nullptr), "database_unavailable");
    return ctx;
}
static Json axes(PJ_CONTEXT* ctx, PJ* crs) {
    auto cs = object(proj_crs_get_coordinate_system(ctx, crs));
    require(bool(cs), "coordinate_system_unavailable");
    Json result = Json::array();
    for (int i = 0; i < proj_cs_get_axis_count(ctx, cs.get()); ++i) {
        const char *name, *abbreviation, *direction, *unit, *authority, *code;
        double factor;
        require(proj_cs_get_axis_info(ctx, cs.get(), i, &name, &abbreviation, &direction,
            &factor, &unit, &authority, &code), "axis_unavailable");
        result.push_back({{"name", text(name)}, {"direction", text(direction)}, {"unit", text(unit)}, {"siFactor", factor}});
    }
    return result;
}
static Json definition(PJ_CONTEXT* ctx, PJ* crs) {
    std::string wkt = text(proj_as_wkt(ctx, crs, PJ_WKT2_2019, nullptr));
    std::string json = text(proj_as_projjson(ctx, crs, nullptr));
    require(!wkt.empty() && !json.empty(), "definition_export_unavailable");
    return {{"wkt2", wkt}, {"projjson", Json::parse(json)}};
}
static double error(PJ_COORD actual, const Json& expected) {
    double maximum = 0;
    for (size_t i = 0; i < expected.size(); ++i) {
        require(std::isfinite(actual.v[i]), "non_finite_result");
        maximum = std::max(maximum, std::abs(actual.v[i] - expected[i].get<double>()));
    }
    return maximum;
}
static void in_domain(const Json& test, const Json& source_axes) {
    if (!test.contains("domain")) return;
    const auto& input = test.at("input");
    double latitude = 0, longitude = 0;
    bool have_latitude = false, have_longitude = false;
    for (size_t i = 0; i < source_axes.size(); ++i) {
        if (source_axes[i]["direction"] == "north" && source_axes[i]["unit"] == "degree") {
            latitude = input.at(i).get<double>(); have_latitude = true;
        }
        if (source_axes[i]["direction"] == "east" && source_axes[i]["unit"] == "degree") {
            longitude = input.at(i).get<double>(); have_longitude = true;
        }
    }
    require(have_latitude && have_longitude, "domain_axis_policy_unavailable");
    const auto& box = test.at("domain"); // Authored/published fixture envelope, in degrees.
    require(longitude >= box[0].get<double>() && latitude >= box[1].get<double>() &&
            longitude <= box[2].get<double>() && latitude <= box[3].get<double>(), "outside_declared_domain");
}
static Json transform_case(PJ_CONTEXT* ctx, const Json& test) {
    auto source = object(proj_create(ctx, test.at("source").get<std::string>().c_str()));
    auto target = object(proj_create(ctx, test.at("target").get<std::string>().c_str()));
    require(source && target, "crs_unavailable");
    std::string source_body = text(proj_get_celestial_body_name(ctx, source.get()));
    std::string target_body = text(proj_get_celestial_body_name(ctx, target.get()));
    require(!source_body.empty() && source_body == target_body, "body_mismatch");
    for (PJ* crs : {source.get(), target.get()}) {
        auto datum = object(proj_crs_get_datum_forced(ctx, crs));
        if (datum) require(proj_get_type(datum.get()) != PJ_TYPE_DYNAMIC_GEODETIC_REFERENCE_FRAME &&
            proj_get_type(datum.get()) != PJ_TYPE_DYNAMIC_VERTICAL_REFERENCE_FRAME, "dynamic_frame_unqualified");
    }
    Json source_axes = axes(ctx, source.get()), target_axes = axes(ctx, target.get());
    const auto& input = test.at("input");
    require(input.size() == source_axes.size(), "missing_or_extra_coordinate");
    require(!(source_axes.size() == 2 && target_axes.size() == 3), "height_required");
    for (const auto& value : input) {
        require(value.is_number(), "missing_or_non_numeric_coordinate");
        require(std::isfinite(value.get<double>()), "non_finite_input");
    }
    if (test.contains("axisExpectation")) {
        Json directions = Json::array();
        for (auto& axis : source_axes) directions.push_back(axis["direction"]);
        require(directions == test["axisExpectation"], "axis_mismatch");
    }
    in_domain(test, source_axes);
    const char* options[] = {"ALLOW_BALLPARK=NO", "ONLY_BEST=YES", nullptr};
    auto op = object(proj_create_crs_to_crs_from_pj(ctx, source.get(), target.get(), nullptr, options));
    require(bool(op), "operation_unavailable");
    require(!proj_coordoperation_has_ballpark_transformation(ctx, op.get()), "ballpark_forbidden");
    require(proj_coordoperation_get_grid_used_count(ctx, op.get()) == 0, "grid_requires_separate_qualification");
    require(!proj_coordoperation_requires_per_coordinate_input_time(ctx, op.get()), "dynamic_operation_unqualified");
    std::string selected = text(proj_as_proj_string(ctx, op.get(), PJ_PROJ_5, nullptr));
    require(!selected.empty(), "selected_operation_unavailable");
    // Use the selected definition, not a second source/target operation search.
    auto evaluator = object(proj_create(ctx, selected.c_str()));
    require(bool(evaluator), "selected_operation_reload_failed");
    PJ_COORD coordinate = proj_coord(0, 0, 0, HUGE_VAL);
    for (size_t i = 0; i < input.size(); ++i) {
        require(input[i].is_number(), "missing_or_non_numeric_coordinate");
        coordinate.v[i] = input[i].get<double>();
        require(std::isfinite(coordinate.v[i]), "non_finite_input");
    }
    // For 2D -> 2D, z is only an API padding slot and is never returned as a height.
    PJ_COORD output = proj_trans(evaluator.get(), PJ_FWD, coordinate);
    require(proj_errno(evaluator.get()) == 0, "provider_transform_error");
    double maximum = error(output, test.at("expected"));
    require(maximum <= test.at("tolerance").get<double>(), "reference_mismatch");
    PJ_PROJ_INFO info = proj_pj_info(evaluator.get());
    require(info.has_inverse, "inverse_unavailable");
    PJ_COORD restored = proj_trans(evaluator.get(), PJ_INV, output);
    require(proj_errno(evaluator.get()) == 0, "provider_inverse_error");
    double inverse_error = error(restored, input);
    require(inverse_error <= test.at("inverseTolerance").get<double>(), "inverse_mismatch");
    Json values = Json::array();
    for (size_t i = 0; i < target_axes.size(); ++i) values.push_back(output.v[i]);
    Json source_definition = definition(ctx, source.get()), target_definition = definition(ctx, target.get());
    // Both encodings must reload with the same declared axes; no visualization normalization.
    for (auto* crs_definition : {&source_definition, &target_definition}) {
        auto wkt = object(proj_create(ctx, (*crs_definition)["wkt2"].get<std::string>().c_str()));
        auto json = object(proj_create(ctx, (*crs_definition)["projjson"].dump().c_str()));
        require(wkt && json && proj_is_equivalent_to(wkt.get(), json.get(), PJ_COMP_STRICT), "crs_encoding_mismatch");
    }
    double west, south, east, north;
    const char* area_name;
    Json area = nullptr;
    if (proj_get_area_of_use(ctx, op.get(), &west, &south, &east, &north, &area_name))
        area = {{"name", text(area_name)}, {"boundsDegrees", {west, south, east, north}}};
    const double accuracy = proj_coordoperation_get_accuracy(ctx, op.get());
    return {{"actual", values}, {"maxError", maximum}, {"inverseMaxError", inverse_error},
        {"sourceDefinition", source_definition}, {"targetDefinition", target_definition},
        {"sourceAxes", source_axes}, {"targetAxes", target_axes}, {"body", source_body},
        {"selectedOperation", selected}, {"operationDefinition", definition(ctx, op.get())},
        {"gridCount", 0}, {"inverseAvailable", true}, {"providerAreaOfUse", area},
        {"providerAccuracyMetres", accuracy < 0 ? Json(nullptr) : Json(accuracy)},
        {"datasetAccuracy", nullptr}};
}

extern "C" __attribute__((visibility("default")))
int geox_proj_probe_run(const char* directory, const char* fixtures_path, const char* report_path) noexcept {
    Json report = {{"provider", proj_info().version}, {"results", Json::array()}, {"success", false}};
    try {
        auto ctx = context(directory);
        require(!proj_context_is_network_enabled(ctx.get()), "network_not_disabled");
        report["networkEnabled"] = false;
        report["database"] = Json::object();
        for (const char* key : {"PROJ.VERSION", "EPSG.VERSION", "EPSG.DATE", "DATABASE.LAYOUT.VERSION.MAJOR", "DATABASE.LAYOUT.VERSION.MINOR"})
            report["database"][key] = text(proj_context_get_database_metadata(ctx.get(), key));
        Json fixtures; std::ifstream input(fixtures_path); input >> fixtures;
        int failed = 0;
        for (const auto& test : fixtures.at("cases")) {
            Json record = {{"id", test.at("id")}};
            try {
                record["evidence"] = transform_case(ctx.get(), test);
                record["outcome"] = "available";
            } catch (const std::exception& exception) { record["outcome"] = exception.what(); }
            record["passed"] = record["outcome"] == test.at("outcome");
            if (!record["passed"].get<bool>()) failed++;
            report["results"].push_back(record);
        }
        // A mandatory unavailable resource must not turn into identity, even offline.
        auto missing = object(proj_create(ctx.get(), "+proj=hgridshift +grids=geox_qualification_missing.gsb"));
        bool grid_rejected = !missing;
        report["results"].push_back({{"id", "missing-grid"}, {"passed", grid_rejected}});
        if (!grid_rejected) failed++;
        // Failure must not poison a new operation or context; repeat ownership lifecycle.
        for (int i = 0; i < fixtures.at("lifecycleIterations").get<int>(); ++i) {
            auto fresh = context(directory);
            transform_case(fresh.get(), fixtures.at("cases")[0]);
        }
        report["lifecycleIterations"] = fixtures.at("lifecycleIterations");
        report["failed"] = failed;
        report["success"] = failed == 0;
    } catch (const std::exception& exception) { report["error"] = exception.what(); }
    try { std::ofstream output(report_path); output << report.dump(2) << '\n'; if (!output) return 2; }
    catch (...) { return 2; }
    return report["success"].get<bool>() ? 0 : 1;
}
