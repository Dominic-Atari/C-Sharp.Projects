#!/usr/bin/env node
// Usage: node scripts/clear-ui-playwright.js --url http://localhost:8104
// This script opens the provided URL headless, runs a small snippet to clear
// `nile.stories` from localStorage and dispatches the `topics:deleted` event.

const { chromium } = require('playwright');

function parseArgs() {
  const args = process.argv.slice(2);
  const out = {};
  for (let i = 0; i < args.length; i++) {
    const a = args[i];
    if (a.startsWith('--')) {
      const eq = a.indexOf('=');
      if (eq >= 0) {
        out[a.slice(2, eq)] = a.slice(eq + 1);
      } else {
        const key = a.slice(2);
        const next = args[i + 1];
        if (next && !next.startsWith('-')) { out[key] = next; i++; }
        else out[key] = true;
      }
    }
  }
  return out;
}

(async () => {
  const argv = parseArgs();
  const url = argv.url || argv.u || 'http://localhost:8104';
  console.log(`Opening ${url} (headless)...`);
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  try {
    await page.goto(url, { waitUntil: 'networkidle' });
    const result = await page.evaluate(() => {
      try {
        localStorage.removeItem('nile.stories');
        window.dispatchEvent(new CustomEvent('topics:deleted', { detail: { scope: 'ui-clear' } }));
        return { ok: true };
      } catch (e) {
        return { ok: false, error: String(e) };
      }
    });
    if (result && result.ok) console.log('Cleared `nile.stories` and dispatched `topics:deleted`.');
    else console.error('Failed to run snippet on page:', result && result.error);
  } catch (err) {
    console.error('Error while loading page or running snippet:', err);
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
