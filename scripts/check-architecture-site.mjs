import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const read = (path) => readFileSync(resolve(root, path));
const hash = (bytes) => createHash('sha256').update(bytes).digest('hex');
const repository = 'https://github.com/bibi45c/UEA-CMP-SEM2-GAME-Turn_Based_Strategy';
const published = 'https://bibi45c.github.io/UEA-CMP-SEM2-GAME-Turn_Based_Strategy/';
const git = (...args) => execFileSync('git', args, { cwd: root, maxBuffer: 2097152 });
const paths = new Set();

function verifyDiagram(specPath, receiptPath, htmlPath, locale) {
  const specBytes = read(specPath);
  const spec = JSON.parse(specBytes);
  const receipt = JSON.parse(read(receiptPath));
  const html = read(htmlPath);
  const markup = html.toString('utf8');
  const revision = spec.meta.repository.revision;
  assert.equal(spec.diagram_type, 'architecture');
  assert.equal(spec.meta.quality_profile, 'showcase');
  assert.equal(spec.meta.locale, locale);
  assert.equal(spec.meta.repository.url, repository);
  assert.match(revision, /^[a-f0-9]{40}$/);
  assert.equal(receipt.revision, revision);
  assert.equal(receipt.specification.sha256, hash(specBytes), specPath + ': refresh the delivery.');
  assert.equal(receipt.specification.bytes, specBytes.length);
  assert.equal(receipt.artifact.sha256, hash(html), htmlPath + ': differs from accepted delivery.');
  assert.equal(receipt.artifact.bytes, html.length);
  assert.equal(receipt.validation.checksPassed, 9);
  assert.equal(receipt.validation.checkCount, 9);
  assert.equal(receipt.validation.errors, 0);
  assert.equal(receipt.validation.warnings, 0);
  assert.equal(receipt.browser_evidence, 'passed');
  assert.equal(receipt.visual_review, 'passed');
  for (const preview of [receipt.preview, receipt.dark_preview]) {
    assert.match(preview.path, /^Docs\/Architecture_Preview(?:_EN)?(?:_Dark)?\.png$/);
    const bytes = read(preview.path);
    assert.equal(preview.sha256, hash(bytes), preview.path + ': changed after review.');
    assert.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
  }
  for (const component of spec.components) {
    assert.ok(component.sources?.length, component.id + ' needs source evidence.');
    for (const source of component.sources) {
      assert.ok(source.path.startsWith('Assets/_Project/Scripts/'));
      assert.ok(!source.path.includes('..'));
      const pinned = git('show', revision + ':' + source.path);
      // Compare Git blobs rather than Windows CRLF working-tree files.
      assert.ok(pinned.equals(git('show', 'HEAD:' + source.path)), 'Source changed: ' + source.path);
      const lineCount = pinned.toString('utf8').split('\n').length;
      if (source.line) assert.ok(source.line >= 1 && source.line <= lineCount);
      if (source.end_line) assert.ok(source.end_line >= (source.line || 1) && source.end_line <= lineCount);
      paths.add(source.path);
      assert.ok(markup.includes(source.path), 'Missing source: ' + source.path);
    }
  }
  assert.equal((markup.match(/<svg\b/g) || []).length, 1);
  assert.ok(markup.includes(revision));
  assert.ok(markup.includes('<html lang="' + locale + '"'));
  assert.ok(!/<(?:script|link)\b[^>]*(?:src|href)=["'](?:https?:)?\/\//i.test(markup),
    'Viewer must not require remote scripts or stylesheets.');
  if (locale === 'en') assert.ok(!/\p{Unified_Ideograph}/u.test(specBytes.toString('utf8')));
  return spec;
}

const chinese = verifyDiagram('Docs/Architecture_Main_Archify.json', 'Docs/Architecture_Validation.json', 'site/index.html', 'zh-CN');
const english = verifyDiagram('Docs/Architecture_Main_Archify_EN.json', 'Docs/Architecture_Validation_EN.json', 'site/en.html', 'en');
// Translation may adjust label placement, never topology, evidence or node geometry.
const topology = (spec) => ({
  repository: spec.meta.repository,
  layout: spec.layout,
  components: spec.components.map(({ id, type, row, col, size, sources }) => ({
    id, type, row, col, size, sources: sources.map(({ label, ...evidence }) => evidence),
  })),
  connections: spec.connections.map(({ id, from, to, variant }) => ({ id, from, to, variant })),
  views: spec.meta.views.map(({ id, focus }) => ({ id, focus })),
});
assert.deepEqual(topology(chinese), topology(english), 'Languages must describe the same architecture.');
assert.ok(read('site/ARCHIFY-LICENSE.txt').toString('utf8').includes('MIT License'));
const readme = read('README.md').toString('utf8');
for (const local of ['site/index.html', 'site/en.html', 'Docs/Architecture_Preview.png',
  'Docs/Architecture_Preview_Dark.png', 'Docs/Architecture_Preview_EN.png', 'Docs/Architecture_Preview_EN_Dark.png']) {
  assert.ok(readme.includes(local), 'Missing README link: ' + local);
}
assert.ok(readme.includes(published) && readme.includes(published + 'en.html'));
assert.equal((readme.match(/<picture>/g) || []).length, 2);
assert.equal((readme.match(/media="\(prefers-color-scheme: dark\)"/g) || []).length, 2);
assert.ok(readme.includes('<details>') && readme.includes('<summary>'));
const screenshotPaths = new Set([...readme.matchAll(/Docs\/Screenshots\/[a-z0-9-]+\.png/g)].map((match) => match[0]));
assert.ok(screenshotPaths.size > 0, 'README must include gameplay screenshots.');
for (const screenshotPath of screenshotPaths) {
  assert.equal(read(screenshotPath).subarray(0, 8).toString('hex'), '89504e470d0a1a0a', screenshotPath);
}
assert.ok(read('Docs/Screenshots/README.md').toString('utf8').includes('2026-03-04'));
console.log('Architecture site passed: EN/ZH topology, hashes, ' + paths.size + ' source files, ' + screenshotPaths.size + ' gameplay screenshots and theme-aware README links.');
