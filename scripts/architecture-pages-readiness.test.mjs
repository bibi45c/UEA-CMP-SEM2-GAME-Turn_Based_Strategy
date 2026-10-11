import assert from 'node:assert/strict';
import test from 'node:test';
import { checkPages } from './architecture-pages-readiness.mjs';

const options = { token: 'synthetic-test-token', repository: 'example/game' };
const reply = (status, buildType) => async (url, init) => {
  assert.equal(url, 'https://api.github.com/repos/example/game/pages');
  assert.equal(init.method, undefined, 'Only the default GET request is allowed.');
  assert.equal(init.headers.Authorization, 'Bearer synthetic-test-token');
  assert.ok(init.signal instanceof AbortSignal);
  return { status, json: async () => ({ build_type: buildType }) };
};

test('workflow-configured Pages permits deployment', async () => {
  assert.equal((await checkPages({ ...options, request: reply(200, 'workflow') })).enabled, true);
});
test('unavailable Pages skips publication, not validation', async () => {
  const result = await checkPages({ ...options, request: reply(404) });
  assert.equal(result.enabled, false);
  assert.match(result.message, /Not published/);
});
test('existing legacy Pages is not overwritten', async () => {
  assert.equal((await checkPages({ ...options, request: reply(200, 'legacy') })).enabled, false);
});
for (const status of [401, 403, 429, 500]) {
  test(`HTTP ${status} fails rather than silently skipping`, async () => {
    await assert.rejects(checkPages({ ...options, request: reply(status) }), new RegExp(`HTTP ${status}`));
  });
}
test('unknown build type fails closed', async () => {
  await assert.rejects(checkPages({ ...options, request: reply(200, 'unknown') }), /unknown build type/);
});
test('network failure fails rather than silently skipping', async () => {
  await assert.rejects(checkPages({ ...options, request: async () => { throw new Error('network failure'); } }), /network failure/);
});
test('missing credentials or invalid repository fails before a request', async () => {
  let requested = false;
  const request = async () => { requested = true; };
  await assert.rejects(checkPages({ repository: options.repository, request }), /required/);
  await assert.rejects(checkPages({ ...options, repository: '../game', request }), /required/);
  assert.equal(requested, false);
});
