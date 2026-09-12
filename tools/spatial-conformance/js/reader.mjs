/**
 * Provider-free reader for the portable scientific scene v2 contract.
 *
 * This module intentionally does not load Unity, PROJ, assets, or network
 * providers. It validates the portable document and evaluates only the
 * declared affine v1 operation when its explicit capability conditions hold.
 */

const NUMBER_TOKEN = /^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$/;
const INTEGER_TOKEN = /^-?(?:0|[1-9][0-9]*)$/;
const NUMERICAL_TOLERANCE = 64 * 2.2204460492503131e-16;

export class ReaderError extends Error {
  constructor(message) {
    super(message);
    this.name = "ReaderError";
  }
}

function fail(message) {
  throw new ReaderError(message);
}

function requireCondition(condition, message) {
  if (!condition) fail(message);
}

function isWhitespace(character) {
  return character === " " || character === "\t" || character === "\r" || character === "\n";
}

/**
 * Scan strict JSON before using the native parser. JSON.parse intentionally
 * keeps only the final duplicate key, so this scanner decodes object keys and
 * rejects duplicates before any value can be lost.
 */
class StrictJsonScanner {
  constructor(text) {
    this.text = text;
    this.index = 0;
  }

  scan() {
    this.skipWhitespace();
    this.scanValue();
    this.skipWhitespace();
    requireCondition(this.index === this.text.length, "trailing_content");
  }

  skipWhitespace() {
    while (this.index < this.text.length && isWhitespace(this.text[this.index])) this.index += 1;
  }

  scanValue() {
    this.skipWhitespace();
    requireCondition(this.index < this.text.length, "missing_json_value");
    const character = this.text[this.index];
    if (character === "{") return this.scanObject();
    if (character === "[") return this.scanArray();
    if (character === '"') {
      this.scanString();
      return;
    }
    this.scanScalar();
  }

  scanObject() {
    this.index += 1;
    const names = new Set();
    this.skipWhitespace();
    if (this.take("}")) return;
    while (true) {
      requireCondition(this.text[this.index] === '"', "unquoted_property");
      const name = this.scanString();
      requireCondition(!names.has(name), "duplicate_json_key: " + name);
      names.add(name);
      this.skipWhitespace();
      requireCondition(this.take(":"), "missing_property_separator");
      this.scanValue();
      this.skipWhitespace();
      if (this.take("}")) return;
      requireCondition(this.take(","), "missing_object_separator");
      this.skipWhitespace();
      requireCondition(this.text[this.index] !== "}", "trailing_comma");
    }
  }

  scanArray() {
    this.index += 1;
    this.skipWhitespace();
    if (this.take("]")) return;
    while (true) {
      this.scanValue();
      this.skipWhitespace();
      if (this.take("]")) return;
      requireCondition(this.take(","), "missing_array_separator");
      this.skipWhitespace();
      requireCondition(this.text[this.index] !== "]", "trailing_comma");
    }
  }

  scanString() {
    requireCondition(this.take('"'), "expected_string");
    let result = "";
    while (this.index < this.text.length) {
      const character = this.text[this.index++];
      requireCondition(character >= " ", "control_character_in_string");
      if (character === '"') return result;
      if (character !== "\\") {
        result += character;
        continue;
      }
      requireCondition(this.index < this.text.length, "unfinished_escape");
      const escape = this.text[this.index++];
      const decoded = {
        '"': '"',
        "\\": "\\",
        "/": "/",
        b: "\b",
        f: "\f",
        n: "\n",
        r: "\r",
        t: "\t",
      }[escape];
      if (decoded !== undefined) {
        result += decoded;
        continue;
      }
      requireCondition(escape === "u", "invalid_escape");
      const hex = this.text.slice(this.index, this.index + 4);
      requireCondition(/^[0-9a-fA-F]{4}$/.test(hex), "invalid_unicode_escape");
      result += String.fromCharCode(Number.parseInt(hex, 16));
      this.index += 4;
    }
    fail("unfinished_string");
  }

  scanScalar() {
    const start = this.index;
    while (this.index < this.text.length) {
      const character = this.text[this.index];
      if (isWhitespace(character) || "{}[],:\"".includes(character)) break;
      this.index += 1;
    }
    const token = this.text.slice(start, this.index);
    requireCondition(token === "true" || token === "false" || token === "null" || NUMBER_TOKEN.test(token),
      "invalid_json_token");
  }

  take(character) {
    if (this.text[this.index] !== character) return false;
    this.index += 1;
    return true;
  }
}

function rawNumberText(value) {
  requireCondition(JSON.isRawJSON(value), "expected_number");
  return JSON.stringify(value);
}

function parseStrictJson(text) {
  requireCondition(typeof text === "string", "expected_json_text");
  new StrictJsonScanner(text).scan();
  const parsed = JSON.parse(text, (key, value, context) => {
    if (context.source !== undefined && NUMBER_TOKEN.test(context.source)) return JSON.rawJSON(context.source);
    return value;
  });
  checkValues(parsed);
  return parsed;
}

function checkValues(value, path = "") {
  if (JSON.isRawJSON(value)) {
    // JSON.NET retains arbitrary-size integer tokens, but parses decimal and
    // exponent spellings as binary64 floats. Match that distinction here.
    if (!INTEGER_TOKEN.test(rawNumberText(value))) number(value, path);
    return;
  }
  if (Array.isArray(value)) {
    value.forEach((item, index) => checkValues(item, path + "[" + index + "]"));
    return;
  }
  if (value !== null && typeof value === "object") {
    for (const [key, item] of Object.entries(value)) checkValues(item, path ? path + "." + key : key);
    return;
  }
  requireCondition(typeof value === "string" || typeof value === "boolean" || value === null, "non_json_value");
}

function object(value, path = "") {
  requireCondition(value !== null && typeof value === "object" && !Array.isArray(value) && !JSON.isRawJSON(value),
    "expected_object: " + path);
  return value;
}

function array(value, path = "") {
  requireCondition(Array.isArray(value), "expected_array: " + path);
  return value;
}

function shape(value, fields, path = "") {
  const record = object(value, path);
  const actual = Object.keys(record).sort();
  const expected = [...fields].sort();
  requireCondition(actual.length === expected.length && actual.every((name, index) => name === expected[index]),
    "unexpected_fields: " + path);
  return record;
}

function text(value, { blankAllowed = false, nullable = false, path = "" } = {}) {
  if (nullable && value === null) return null;
  requireCondition(typeof value === "string", "expected_string: " + path);
  requireCondition(blankAllowed || value.trim().length > 0, "blank_identifier: " + path);
  return value;
}

function boolean(value, path = "") {
  requireCondition(typeof value === "boolean", "expected_boolean: " + path);
  return value;
}

function number(value, path = "") {
  const digits = rawNumberText(value);
  const parsed = Number(digits);
  requireCondition(Number.isFinite(parsed), "non_finite_number: " + path);
  return parsed;
}

function positiveInteger(value, path = "") {
  const digits = rawNumberText(value);
  requireCondition(INTEGER_TOKEN.test(digits), "expected_integer: " + path);
  requireCondition(digits !== "0" && !digits.startsWith("-"), "expected_positive_integer");
}

function hasVersion(value, expected) {
  return JSON.isRawJSON(value) && rawNumberText(value) === String(expected);
}

function choice(value, choices, path = "") {
  const selected = text(value, { path });
  requireCondition(choices.includes(selected), "unexpected_enum: " + path);
  return selected;
}

function table(value, validate, path) {
  const records = new Map();
  array(value, path).forEach((record, index) => {
    const itemPath = path + "[" + index + "]";
    const item = object(record, itemPath);
    const id = text(item.id, { path: itemPath + ".id" });
    requireCondition(!records.has(id), "duplicate_identity: " + path + ": " + id);
    validate?.(item, itemPath);
    records.set(id, item);
  });
  return records;
}

function reference(value, records, { nullable = false, path = "" } = {}) {
  const id = text(value, { nullable, path });
  requireCondition(id === null || records.has(id), "dangling_reference: " + path);
  return id;
}

function definition(value, path) {
  const record = shape(value, ["encoding", "value"], path);
  const encoding = choice(record.encoding, ["opaque", "wkt2", "projjson"], path + ".encoding");
  const validValue = typeof record.value === "string" ||
    (record.value !== null && typeof record.value === "object" && !Array.isArray(record.value) &&
      !JSON.isRawJSON(record.value));
  requireCondition(validValue, "invalid_definition_value");
  if (encoding === "wkt2") requireCondition(typeof record.value === "string" && record.value.length > 0,
    "invalid_wkt2_value");
  if (encoding === "projjson") object(record.value, path + ".value");
}

function evidence(value, path) {
  const record = shape(value, ["origins", "lineage", "statements"], path);
  const origins = new Set();
  array(record.origins, path + ".origins").forEach((origin, index) => {
    const item = choice(origin, ["measured", "reconstructed", "interpreted", "generated", "mixed"],
      path + ".origins[" + index + "]");
    requireCondition(!origins.has(item), "duplicate_value: " + path + ".origins");
    origins.add(item);
  });
  array(record.lineage, path + ".lineage").forEach((lineage, index) => object(lineage,
    path + ".lineage[" + index + "]"));
  array(record.statements, path + ".statements").forEach((statement, index) => {
    const statementPath = path + ".statements[" + index + "]";
    const item = shape(statement, ["category", "status", "details"], statementPath);
    choice(item.category, ["source-accuracy", "fit-residual", "operation-accuracy", "tracking",
      "representation-error", "declaration", "legacy"], statementPath + ".category");
    choice(item.status, ["supplied", "derived", "independently-checked", "unknown"],
      statementPath + ".status");
    object(item.details, statementPath + ".details");
  });
}

function locator(value) {
  requireCondition(!/[\s\x00-\x1f\\?#]/.test(value), "unsafe_locator");
  if (/^https:\/\//i.test(value)) {
    let url;
    try {
      url = new URL(value);
    } catch {
      fail("unsafe_https_locator");
    }
    requireCondition(url.protocol === "https:" && url.hostname.length > 0 && url.username.length === 0 &&
      url.password.length === 0, "unsafe_https_locator");
    return;
  }
  requireCondition(!value.startsWith("/") && !value.startsWith("~") && !value.includes(":"),
    "unsafe_package_locator");
  requireCondition(!value.includes("%") && value.split("/").every((part) => part.length > 0 &&
    part !== "." && part !== ".."), "unsafe_package_locator");
}

function validateAsset(asset, path) {
  const record = shape(asset, ["id", "contentHash", "format", "locators", "metadata", "evidence"], path);
  text(record.format, { nullable: true, path: path + ".format" });
  object(record.metadata, path + ".metadata");
  evidence(record.evidence, path + ".evidence");
  if (record.contentHash !== null) {
    const hash = shape(record.contentHash, ["algorithm", "value"], path + ".contentHash");
    text(hash.algorithm, { path: path + ".contentHash.algorithm" });
    text(hash.value, { path: path + ".contentHash.value" });
  }
  array(record.locators, path + ".locators").forEach((item, index) => locator(text(item,
    { path: path + ".locators[" + index + "]" })));
}

function inverseLinear(linear) {
  const scale = Math.max(...linear.map((value) => Math.abs(value)));
  if (scale === 0) fail("singular_registration");
  const scaled = linear.map((value) => value / scale);
  const determinant = determinant3(scaled);
  if (determinant === 0) fail("singular_registration");
  const inverse = [
    scaled[4] * scaled[8] - scaled[5] * scaled[7], scaled[2] * scaled[7] - scaled[1] * scaled[8], scaled[1] * scaled[5] - scaled[2] * scaled[4],
    scaled[5] * scaled[6] - scaled[3] * scaled[8], scaled[0] * scaled[8] - scaled[2] * scaled[6], scaled[2] * scaled[3] - scaled[0] * scaled[5],
    scaled[3] * scaled[7] - scaled[4] * scaled[6], scaled[1] * scaled[6] - scaled[0] * scaled[7], scaled[0] * scaled[4] - scaled[1] * scaled[3],
  ].map((value) => value / determinant);
  const infinityNorm = (matrix) => Math.max(
    Math.abs(matrix[0]) + Math.abs(matrix[1]) + Math.abs(matrix[2]),
    Math.abs(matrix[3]) + Math.abs(matrix[4]) + Math.abs(matrix[5]),
    Math.abs(matrix[6]) + Math.abs(matrix[7]) + Math.abs(matrix[8]),
  );
  const condition = infinityNorm(scaled) * infinityNorm(inverse);
  if (!Number.isFinite(condition) || condition * NUMERICAL_TOLERANCE >= 1) fail("ill_conditioned_registration");
  return inverse.map((value) => {
    const result = value / scale;
    requireCondition(Number.isFinite(result), "non_finite_coordinate");
    return result;
  });
}

function determinant3(matrix) {
  return matrix[0] * (matrix[4] * matrix[8] - matrix[5] * matrix[7]) -
    matrix[1] * (matrix[3] * matrix[8] - matrix[5] * matrix[6]) +
    matrix[2] * (matrix[3] * matrix[7] - matrix[4] * matrix[6]);
}

function affine(operation, path) {
  const payload = object(operation.payload, path + ".payload");
  const linear = array(payload.linear, path + ".payload.linear");
  const translation = array(payload.translation, path + ".payload.translation");
  requireCondition(linear.length === 9 && translation.length === 3, "invalid_affine_dimensions");
  const matrix = linear.map((value, index) => number(value, path + ".payload.linear[" + index + "]"));
  const offset = translation.map((value, index) => number(value, path + ".payload.translation[" + index + "]"));
  inverseLinear(matrix);
  return { linear: matrix, translation: offset };
}

function isAffine(operation) {
  return operation.kind === "affine" && hasVersion(operation.version, 1);
}

function validateOperation(operation, path) {
  const record = shape(operation, ["id", "kind", "version", "sourceFrameId", "targetFrameId", "payload", "evidence"], path);
  text(record.kind, { path: path + ".kind" });
  positiveInteger(record.version, path + ".version");
  object(record.payload, path + ".payload");
  evidence(record.evidence, path + ".evidence");
  if (isAffine(record)) {
    shape(record.payload, ["linear", "translation"], path + ".payload");
    affine(record, path);
  }
  if (record.kind === "geodetic" && hasVersion(record.version, 1)) {
    const payload = shape(record.payload, ["definition", "provider", "database", "resources", "domain", "inverseAvailable"],
      path + ".payload");
    definition(payload.definition, path + ".payload.definition");
    const provider = shape(payload.provider, ["name", "version"], path + ".payload.provider");
    text(provider.name, { path: path + ".payload.provider.name" });
    text(provider.version, { path: path + ".payload.provider.version" });
    object(payload.database, path + ".payload.database");
    array(payload.resources, path + ".payload.resources").forEach((resource, index) => object(resource,
      path + ".payload.resources[" + index + "]"));
    object(payload.domain, path + ".payload.domain");
    boolean(payload.inverseAvailable, path + ".payload.inverseAvailable");
  }
}

function validateFrame(frame, path) {
  const record = shape(frame, ["id", "body", "kind", "definition", "cartesian"], path);
  text(record.body, { nullable: true, path: path + ".body" });
  choice(record.kind, ["unknown", "cartesian", "geographic"], path + ".kind");
  definition(record.definition, path + ".definition");
  if (record.cartesian === null) return;
  const cartesian = shape(record.cartesian, ["axisConvention", "metresPerUnit"], path + ".cartesian");
  text(cartesian.axisConvention, { path: path + ".cartesian.axisConvention" });
  if (cartesian.metresPerUnit !== null) requireCondition(number(cartesian.metresPerUnit,
    path + ".cartesian.metresPerUnit") > 0, "non_positive_unit_factor");
}

function validateRegistration(record, path) {
  const item = shape(record, ["id", "operationId", "supersedes", "evidence"], path);
  evidence(item.evidence, path + ".evidence");
}

function validateLayer(record, path) {
  const item = shape(record, ["id", "name", "sourceFrameId", "activeRegistrationId", "sourceAssetIds", "representations", "metadata", "evidence"], path);
  text(item.name, { blankAllowed: true, path: path + ".name" });
  object(item.metadata, path + ".metadata");
  evidence(item.evidence, path + ".evidence");
  const assets = new Set();
  array(item.sourceAssetIds, path + ".sourceAssetIds").forEach((asset, index) => {
    const id = text(asset, { path: path + ".sourceAssetIds[" + index + "]" });
    requireCondition(!assets.has(id), "duplicate_value: " + path + ".sourceAssetIds");
    assets.add(id);
  });
}

function validateRepresentation(record, path) {
  shape(record, ["id", "assetId", "frameId", "toSourceOperationId"], path);
}

function validateHistory(registrations, operations) {
  for (const registration of registrations.values()) {
    reference(registration.operationId, operations, { path: "registrations.operationId" });
    reference(registration.supersedes, registrations, { nullable: true, path: "registrations.supersedes" });
  }
  const completed = new Set();
  for (const start of registrations.keys()) {
    const path = new Set();
    let id = start;
    while (id !== null && !completed.has(id)) {
      requireCondition(!path.has(id), "cyclic_registration_history");
      path.add(id);
      id = registrations.get(id).supersedes;
    }
    for (const id of path) completed.add(id);
  }
}

function validateDocument(document) {
  const scene = shape(document, ["schemaVersion", "id", "revision", "sceneFrameId", "frames", "assets", "operations", "registrations", "layers", "metadata", "extensions"], "");
  requireCondition(hasVersion(scene.schemaVersion, 2), "unsupported_scene_schema");
  text(scene.id, { path: "id" });
  text(scene.revision, { path: "revision" });
  object(scene.metadata, "metadata");
  const frames = table(scene.frames, validateFrame, "frames");
  const assets = table(scene.assets, validateAsset, "assets");
  const operations = table(scene.operations, validateOperation, "operations");
  const registrations = table(scene.registrations, validateRegistration, "registrations");
  const layers = table(scene.layers, validateLayer, "layers");
  const common = reference(scene.sceneFrameId, frames, { path: "sceneFrameId" });
  for (const [namespace, extension] of Object.entries(object(scene.extensions, "extensions"))) {
    requireCondition(namespace.trim().length > 0, "blank_extension_namespace");
    const record = shape(extension, ["version", "required", "payload"], "extensions." + namespace);
    positiveInteger(record.version, "extensions." + namespace + ".version");
    boolean(record.required, "extensions." + namespace + ".required");
  }
  for (const operation of operations.values()) {
    const source = reference(operation.sourceFrameId, frames, { path: "operations.sourceFrameId" });
    const target = reference(operation.targetFrameId, frames, { path: "operations.targetFrameId" });
    if (source === target && isAffine(operation)) {
      const map = affine(operation, "operations." + operation.id);
      requireCondition(map.linear.every((value, index) => value === [1, 0, 0, 0, 1, 0, 0, 0, 1][index]) &&
        map.translation.every((value) => value === 0), "same_frame_requires_identity");
    }
  }
  validateHistory(registrations, operations);
  for (const layer of layers.values()) {
    const source = reference(layer.sourceFrameId, frames, { path: "layers.sourceFrameId" });
    const registration = reference(layer.activeRegistrationId, registrations, { nullable: true, path: "layers.activeRegistrationId" });
    if (registration !== null) endpoints(operations.get(registrations.get(registration).operationId), source, common);
    for (const assetId of array(layer.sourceAssetIds, "layers.sourceAssetIds")) reference(assetId, assets,
      { path: "layers.sourceAssetIds" });
    const representations = table(layer.representations, validateRepresentation, "layers.representations");
    for (const representation of representations.values()) {
      reference(representation.assetId, assets, { path: "representations.assetId" });
      const frame = reference(representation.frameId, frames, { path: "representations.frameId" });
      const operation = reference(representation.toSourceOperationId, operations,
        { nullable: true, path: "representations.toSourceOperationId" });
      if (operation !== null) endpoints(operations.get(operation), frame, source);
    }
  }
  return { scene, frames, assets, operations, registrations, layers };
}

function endpoints(operation, source, target) {
  requireCondition(operation.sourceFrameId === source && operation.targetFrameId === target,
    "operation_endpoint_mismatch: " + operation.id);
}

function apply(map, point) {
  return [
    map.linear[0] * point[0] + map.linear[1] * point[1] + map.linear[2] * point[2] + map.translation[0],
    map.linear[3] * point[0] + map.linear[4] * point[1] + map.linear[5] * point[2] + map.translation[1],
    map.linear[6] * point[0] + map.linear[7] * point[1] + map.linear[8] * point[2] + map.translation[2],
  ].map((value) => {
    requireCondition(Number.isFinite(value), "non_finite_coordinate");
    return value;
  });
}

function inverse(map) {
  const linear = inverseLinear(map.linear);
  const translation = apply({ linear, translation: [0, 0, 0] }, map.translation).map((value) => -value);
  return { linear, translation };
}

function requiredExtensionReason(scene) {
  return Object.values(scene.extensions).some((extension) => extension.required) ? "unsupported_required_extension" : null;
}

function tryAffine(state, operationId) {
  const operation = state.operations.get(operationId);
  if (operation === undefined) return { available: false, reason: "unknown_record_identity" };
  const extensionReason = requiredExtensionReason(state.scene);
  if (extensionReason !== null) return { available: false, reason: extensionReason };
  if (!isAffine(operation)) {
    return {
      available: false,
      reason: operation.kind === "geodetic" && hasVersion(operation.version, 1)
        ? "qualified_geodetic_provider_required"
        : "unsupported_operation_kind_or_version",
    };
  }
  const source = state.frames.get(operation.sourceFrameId);
  const target = state.frames.get(operation.targetFrameId);
  if (source.body !== null && target.body !== null && source.body !== target.body)
    return { available: false, reason: "known_body_conflict" };
  if (source.kind === "geographic" || target.kind === "geographic")
    return { available: false, reason: "geographic_frame_requires_qualified_provider" };
  if (source.definition.encoding !== "opaque" || target.definition.encoding !== "opaque")
    return { available: false, reason: "crs_interpretation_required" };
  return { available: true, reason: null, map: affine(operation, "operations." + operation.id) };
}

function evaluateOperation(state, operationId, point, useInverse) {
  const capability = tryAffine(state, operationId);
  if (!capability.available) fail(capability.reason);
  return apply(useInverse ? inverse(capability.map) : capability.map, point);
}

function sourceToScene(state, layerId, point, useInverse) {
  const layer = state.layers.get(layerId);
  if (layer === undefined) fail("unknown_record_identity");
  if (layer.activeRegistrationId === null) fail("layer_not_registered");
  const common = state.frames.get(state.scene.sceneFrameId);
  if (common.kind !== "cartesian" || common.cartesian === null)
    fail("common_frame_not_available_for_cartesian_evaluation");
  return evaluateOperation(state, state.registrations.get(layer.activeRegistrationId).operationId, point, useInverse);
}

function representationToSource(state, layerId, representationId, point, useInverse) {
  const layer = state.layers.get(layerId);
  if (layer === undefined) fail("unknown_record_identity");
  const representations = table(layer.representations, validateRepresentation, "layers.representations");
  const representation = representations.get(representationId);
  if (representation === undefined) fail("unknown_record_identity");
  const extensionReason = requiredExtensionReason(state.scene);
  if (extensionReason !== null) fail(extensionReason);
  if (representation.toSourceOperationId !== null)
    return evaluateOperation(state, representation.toSourceOperationId, point, useInverse);
  if (representation.frameId !== layer.sourceFrameId) fail("representation_mapping_unresolved");
  return point;
}

function readPoint(value) {
  const coordinates = array(value, "point");
  requireCondition(coordinates.length === 3, "expected_three_coordinate_point");
  return coordinates.map((coordinate, index) => number(coordinate, "point[" + index + "]"));
}

function validateRequest(request) {
  const input = object(request, "");
  for (const key of Object.keys(input)) requireCondition(["document", "operationId", "layerId", "representationId", "point", "inverse"].includes(key),
    "unexpected_input_field: " + key);
  requireCondition(Object.hasOwn(input, "document"), "expected_object: document");
  object(input.document, "document");
  const hasOperation = Object.hasOwn(input, "operationId");
  const hasLayer = Object.hasOwn(input, "layerId");
  const hasRepresentation = Object.hasOwn(input, "representationId");
  requireCondition(!(hasOperation && hasLayer), "multiple_coordinate_operations");
  if (hasOperation) text(input.operationId, { path: "operationId" });
  if (hasLayer) text(input.layerId, { path: "layerId" });
  requireCondition(!hasRepresentation || hasLayer, "representation_requires_layer_id");
  if (hasRepresentation) text(input.representationId, { path: "representationId" });
  const hasSelector = hasOperation || hasLayer;
  requireCondition(!Object.hasOwn(input, "point") || hasSelector, "point_requires_operation_id");
  requireCondition(!Object.hasOwn(input, "inverse") || hasSelector, "inverse_requires_operation_id");
  requireCondition(!hasLayer || Object.hasOwn(input, "point"), "layer_mapping_requires_point");
  if (Object.hasOwn(input, "inverse")) boolean(input.inverse, "inverse");
  if (Object.hasOwn(input, "point")) readPoint(input.point);
  return input;
}

function unavailable(error) {
  return { available: false, reason: error instanceof ReaderError ? error.message : String(error) };
}

/** Read one JSON wrapper and return the shared conformance response shape. */
export function readRequest(raw) {
  try {
    const request = validateRequest(parseStrictJson(raw));
    const state = validateDocument(request.document);
    const response = { status: "valid", document: state.scene };
    const useInverse = Object.hasOwn(request, "inverse") && request.inverse;
    if (Object.hasOwn(request, "operationId")) {
      const capability = tryAffine(state, request.operationId);
      response.available = capability.available;
      response.reason = capability.reason;
      if (capability.available && Object.hasOwn(request, "point")) {
        const point = readPoint(request.point);
        response.point = apply(useInverse ? inverse(capability.map) : capability.map, point);
      }
    } else if (Object.hasOwn(request, "layerId")) {
      try {
        const point = readPoint(request.point);
        const result = Object.hasOwn(request, "representationId")
          ? representationToSource(state, request.layerId, request.representationId, point, useInverse)
          : sourceToScene(state, request.layerId, point, useInverse);
        response.available = true;
        response.reason = null;
        if (Object.hasOwn(request, "point")) response.point = result;
      } catch (error) {
        Object.assign(response, unavailable(error));
      }
    }
    return response;
  } catch (error) {
    return { status: "invalid", error: error instanceof ReaderError ? error.message : error.message };
  }
}
