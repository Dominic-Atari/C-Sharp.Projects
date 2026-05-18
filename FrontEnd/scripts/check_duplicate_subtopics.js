const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Use actual dev server port discovered earlier
  const appUrl = 'http://localhost:52040/teacher';

  // Create two server-backed stories (use GUIDs to be recognized as server ids)
  const s1 = { id: '11111111-1111-1111-1111-111111111111', promptId: 'p1', subjectId: '5E2344C0-3AE3-41C9-94AA-5FE5DA66D4B7', user: 'Maths', timeAgo: '1m ago', reactions: 0, remixes: 0, extraSubtopics: [] };
  const s2 = { id: '22222222-2222-2222-2222-222222222222', promptId: 'p1', subjectId: '2EDF7A83-E526-4D49-9C19-836D8765F587', user: 'Science', timeAgo: '1m ago', reactions: 0, remixes: 0, extraSubtopics: [] };

  await page.goto('http://localhost:52040', { waitUntil: 'domcontentloaded' });
  // Seed stories in localStorage
  await page.evaluate((stories) => {
    localStorage.setItem('nile.stories', JSON.stringify(stories));
  }, [s1, s2]);

  // Navigate to teacher page
  await page.goto(appUrl, { waitUntil: 'networkidle' });
  await page.waitForTimeout(500);

  // Fire topics:created for first subject
  await page.evaluate(() => {
    window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: 't-a', subtopic: 'Multiplication', subjectId: '5E2344C0-3AE3-41C9-94AA-5FE5DA66D4B7', schoolId: 'F47C42EF-1109-4617-BC0F-A3069C25252F' } }));
    window.dispatchEvent(new CustomEvent('topics:created', { detail: { topicId: 't-b', subtopic: 'Multiplication', subjectId: '2EDF7A83-E526-4D49-9C19-836D8765F587', schoolId: 'F47C42EF-1109-4617-BC0F-A3069C25252F' } }));
  });

  await page.waitForTimeout(500);

  // Extract subtopic pills per story
  const results = await page.evaluate(() => {
    const stories = Array.from(document.querySelectorAll('.story-card'));
    return stories.map((card) => {
      const title = card.querySelector('.name')?.textContent?.trim() || '';
      const pills = Array.from(card.querySelectorAll('.subtopics-strip .pill')).map(p => p.textContent?.trim()).filter(Boolean);
      return { title, pills };
    });
  });

  console.log(JSON.stringify(results, null, 2));

  // Basic assertions
  const maths = results.find(r => r.title === 'Maths');
  const science = results.find(r => r.title === 'Science');

  const mathsHas = maths && maths.pills.filter(p => p === 'Multiplication').length;
  const scienceHas = science && science.pills.filter(p => p === 'Multiplication').length;

  console.log('Maths Multiplication count:', mathsHas, 'Science Multiplication count:', scienceHas);

  if (mathsHas !== 1 || scienceHas !== 1) {
    console.error('Duplicate subtopic check failed');
    await browser.close();
    process.exit(2);
  }

  console.log('Duplicate subtopic check passed ✅');
  await browser.close();
  process.exit(0);
})();
