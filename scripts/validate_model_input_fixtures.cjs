#!/usr/bin/env node
// Run the pinned official Khronos validator, without installing npm dependencies.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '..');
const work = path.join(root, 'build/gltf-importer-trial');
const directory = path.join(root, 'tests/fixtures/model-inputs/generated');
const validator = require(path.join(work, 'validator/package'));
const expectedVersion = '2.0.0-dev.3.10';
const hash = data => crypto.createHash('sha256').update(data).digest('hex');

async function main() {
    if (validator.version() !== expectedVersion) throw new Error('Unqualified validator version');
    const manifest = JSON.parse(fs.readFileSync(path.join(directory, 'manifest.json')));
    const records = [];
    const valid = new Set(['triangle.glb', 'hierarchy.glb', 'textured.glb',
        'external-image.glb', 'multifile.gltf']);
    for (const item of manifest.files) {
        if (path.basename(item.path) !== item.path) throw new Error('Unexpected fixture path');
        const data = fs.readFileSync(path.join(directory, item.path));
        if (hash(data) !== item.sha256) throw new Error('Fixture hash mismatch: ' + item.path);
        if (!/\.(glb|gltf)$/.test(item.path)) continue;
        const record = {file: item.path, sha256: item.sha256, expected: item.expected};
        try {
            record.report = await validator.validateBytes(new Uint8Array(data), {
                uri: item.path,
                externalResourceFunction: async uri => {
                    // Only the authored, declared companions; never network or arbitrary files.
                    if (!['pixel.png', 'geometry.bin'].includes(uri)) throw new Error('Fixture companion unavailable');
                    const resource = fs.readFileSync(path.join(directory, uri));
                    const entry = manifest.files.find(x => x.path === uri);
                    if (!entry || hash(resource) !== entry.sha256) throw new Error('Companion hash mismatch');
                    return new Uint8Array(resource);
                }
            });
            const issues = record.report.issues;
            record.passed = valid.has(item.path) ? issues.numErrors === 0
                : item.path === 'unsupported-required.glb'
                    ? issues.messages.some(m => m.code === 'UNSUPPORTED_EXTENSION')
                    : issues.numErrors > 0;
        } catch (error) {
            record.error = String(error);
            record.passed = item.path === 'bad-magic.glb';
        }
        records.push(record);
    }
    const receipt = {validator: validator.version(), node: process.version,
        validatorArchiveSha256: hash(fs.readFileSync(path.join(work, 'gltf-validator.tgz'))),
        manifestSha256: hash(fs.readFileSync(path.join(directory, 'manifest.json'))),
        success: records.every(r => r.passed), records};
    fs.mkdirSync(path.join(root, 'docs/contracts'), {recursive: true});
    fs.writeFileSync(path.join(root, 'docs/contracts/model-input-validation-2026-09-09.json'),
        JSON.stringify(receipt, null, 2) + '\n');
    for (const r of records) console.log(JSON.stringify({file: r.file, passed: r.passed,
        errors: r.report?.issues.numErrors, warnings: r.report?.issues.numWarnings, error: r.error}));
    if (!receipt.success) process.exitCode = 1;
}
main().catch(error => { console.error(error); process.exitCode = 1; });
