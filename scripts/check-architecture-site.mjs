import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const read = (path) => readFileSync(resolve(root, path));
const hash = (bytes) => createHash('sha256').update(bytes).digest('hex');
const specBytes = read('Docs/Architecture_Main_Archify.json');
const spec = JSON.parse(specBytes);
const receipt = JSON.parse(read('Docs/Architecture_Validation.json'));
const html = read('site/index.html');
const preview = read('Docs/Architecture_Preview.png');
const revision = spec.meta.repository.revision;
const repository = 'https://github.com/bibi45c/UEA-CMP-SEM2-GAME-Turn_Based_Strategy';
const git = (...args) => execFileSync('git', args, { cwd: root, maxBuffer: 2097152 });

assert.equal(spec.diagram_type, 'architecture');
assert.equal(spec.meta.quality_profile, 'showcase');
assert.equal(spec.meta.repository.url, repository);
assert.match(revision, /^[a-f0-9]{40}$/);
assert.equal(receipt.revision, revision);
assert.equal(receipt.specification.sha256, hash(specBytes), 'Regenerate HTML and receipt after changing JSON.');
assert.equal(receipt.specification.bytes, specBytes.length);
assert.equal(receipt.artifact.sha256, hash(html), 'HTML differs from the accepted Archify delivery.');
assert.equal(receipt.artifact.bytes, html.length);
assert.equal(receipt.preview.sha256, hash(preview), 'Preview differs from the reviewed browser capture.');
assert.equal(receipt.validation.checksPassed, 9);
assert.equal(receipt.validation.checkCount, 9);
assert.equal(receipt.validation.errors, 0);
assert.equal(receipt.validation.warnings, 0);
assert.equal(receipt.browser_evidence, 'passed');
assert.equal(receipt.visual_review, 'passed');

const paths = new Set();
for (const component of spec.components) {
  assert.ok(component.sources?.length, component.id + ' needs source evidence.');
  for (const source of component.sources) {
    assert.ok(source.path.startsWith('Assets/_Project/Scripts/'));
    assert.ok(!source.path.includes('..'));
    const pinned = git('show', revision + ':' + source.path);
    // Compare Git blobs to avoid Windows working-tree CRLF differences.
    const current = git('show', 'HEAD:' + source.path);
    assert.ok(pinned.equals(current), 'Source changed: ' + source.path + '. Refresh the architecture snapshot.');
    const lineCount = pinned.toString('utf8').split('\n').length;
    if (source.line) assert.ok(source.line >= 1 && source.line <= lineCount);
    if (source.end_line) assert.ok(source.end_line >= (source.line || 1) && source.end_line <= lineCount);
    paths.add(source.path);
    assert.ok(html.includes(source.path), 'Missing source in viewer: ' + source.path);
  }
}
assert.equal((html.toString('utf8').match(/<svg\b/g) || []).length, 1);
assert.ok(html.includes(Buffer.from(revision)), 'Viewer must retain its evidence revision.');
assert.ok(!/<(?:script|link)\b[^>]*(?:src|href)=["'](?:https?:)?\/\//i.test(html.toString('utf8')),
  'Viewer must not require remote scripts or stylesheets.');
assert.equal(preview.subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
assert.ok(read('site/ARCHIFY-LICENSE.txt').toString('utf8').includes('MIT License'));
const readme = read('README.md').toString('utf8');
assert.ok(readme.includes('Docs/Architecture_Preview.png'));
assert.ok(readme.includes('site/index.html'));
assert.ok(readme.includes('https://bibi45c.github.io/UEA-CMP-SEM2-GAME-Turn_Based_Strategy/'));
const screenshotPaths = new Set([...readme.matchAll(/Docs\/Screenshots\/[a-z0-9-]+\.png/g)].map((match) => match[0]));
assert.ok(screenshotPaths.size > 0, 'README must include gameplay screenshots.');
for (const screenshotPath of screenshotPaths) {
  assert.equal(read(screenshotPath).subarray(0, 8).toString('hex'), '89504e470d0a1a0a', screenshotPath);
}
assert.ok(read('Docs/Screenshots/README.md').toString('utf8').includes('2026-03-04'));
console.log('Architecture site passed: verified hashes, ' + paths.size + ' source files, ' + screenshotPaths.size + ' gameplay screenshots, README links and self-contained HTML.');
