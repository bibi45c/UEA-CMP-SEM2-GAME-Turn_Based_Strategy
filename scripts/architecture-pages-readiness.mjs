import { appendFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

// Read-only: never enable Pages or replace an existing branch-based publication.
export async function checkPages({ token, repository, apiUrl = 'https://api.github.com', request = fetch }) {
  if (!token || !/^[a-z0-9][a-z0-9-]*\/[a-z0-9_][a-z0-9_.-]*$/i.test(repository || '')) {
    throw new Error('A token and owner/repository are required for the Pages check.');
  }
  const response = await request(`${apiUrl}/repos/${repository}/pages`, {
    headers: { Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28' },
    signal: AbortSignal.timeout(30000),
  });
  if (response.status === 404) {
    return { enabled: false, message: 'Not published: Pages is unavailable to this workflow. Enable Pages with Source = GitHub Actions, then rerun on main.' };
  }
  if (response.status !== 200) {
    // Do not mistake rate limits, permission errors or outages for disabled Pages.
    throw new Error(`Pages readiness request failed (HTTP ${response.status}).`);
  }
  const page = await response.json();
  if (page.build_type === 'workflow') {
    return { enabled: true, message: 'Pages is configured for GitHub Actions; deployment may proceed.' };
  }
  if (page.build_type === 'legacy') {
    return { enabled: false, message: 'Not published: existing branch-based Pages configuration is unchanged. Select Source = GitHub Actions before rerunning on main.' };
  }
  throw new Error('Pages returned an unknown build type; refusing to deploy.');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const result = await checkPages({ token: process.env.GH_TOKEN,
    repository: process.env.GITHUB_REPOSITORY, apiUrl: process.env.GITHUB_API_URL });
  if (!process.env.GITHUB_OUTPUT || !process.env.GITHUB_STEP_SUMMARY) {
    throw new Error('This entry point requires GitHub Actions output and summary files.');
  }
  appendFileSync(process.env.GITHUB_OUTPUT, `enabled=${result.enabled}\n`);
  appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Architecture publication\n\n${result.message}\n`);
  console.log(result.message);
}
