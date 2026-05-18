const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();
  const appUrl = 'http://localhost:8105/teacher';

  // Prepare a fake story payload
  const story = {
    id: 's999',
    promptId: 'p1',
    subjectId: 'test-subject-123',
    user: 'SmokeTestTopic',
    timeAgo: 'just now',
    reactions: 0,
    remixes: 0,
    extraSubtopics: []
  };

  // Navigate to app origin then set localStorage so it's same-origin
  await page.goto('http://localhost:8105', { waitUntil: 'domcontentloaded' });
  await page.evaluate((s) => {
    localStorage.setItem('nile.stories', JSON.stringify([s]));
  }, story);

  // Now navigate to teacher page
  await page.goto(appUrl, { waitUntil: 'networkidle' });

  // Give Angular some time to initialize
  await page.waitForTimeout(1000);

  // Read back localStorage
  const stored = await page.evaluate(() => localStorage.getItem('nile.stories'));
  console.log('STORED:', stored);

  // Check presence
  if (!stored) {
    console.error('No stories found in localStorage after page load');
    await browser.close();
    process.exit(2);
  }
  const parsed = JSON.parse(stored);
  const found = Array.isArray(parsed) && parsed.some(s => s && s.id === 's999' && s.user === 'SmokeTestTopic');
  if (!found) {
    console.error('Inserted story missing after page load');
    await browser.close();
    process.exit(3);
  }

  console.log('Story persisted through page load ✅');
  await browser.close();
  process.exit(0);
})();
