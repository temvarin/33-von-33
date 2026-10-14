// Captures the answers of the official catalog from https://oet.bamf.de (manual step, see docs/adr/0006).
//
// The site blocks direct URL access to questions, so this script uses the site's own navigation:
//  1. Open https://oet.bamf.de/ords/oetut/f?p=514:1, choose "Nordrhein-Westfalen", click "Zum Fragenkatalog".
//  2. Paste this script into the browser's developer console on the first question page.
//  3. Wait ~5 minutes (one question per second). The result is copied to the clipboard (Chrome/Edge `copy`);
//     save it as content/bamf-answers.json and run `build`.
//
// Most question texts are images on the site (transcribed into questions-de.txt); for the few that are
// plain HTML the script reports the text so it can be added there.
(async () => {
  const options = [...document.querySelectorAll('#P30_ROWNUM option')]
    .filter(o => o.value !== '%null%')
    .map(o => ({ nr: Number(o.text), id: Number(o.value) }));

  // Load each question in an iframe via the page's own select + submit, so the parent keeps the results.
  const frame = document.createElement('iframe');
  document.body.appendChild(frame);
  const loaded = () => new Promise(resolve => { frame.onload = () => resolve(); });
  frame.src = location.href;
  await loaded();

  const answers = [];
  const htmlTexts = {};
  for (const { nr, id } of options) {
    const next = loaded();
    frame.contentWindow.apex.item('P30_ROWNUM').setValue(String(id), null, true);
    frame.contentWindow.apex.submit('P30_ROWNUM');
    await next;

    const doc = frame.contentDocument;
    const rows = [...doc.querySelectorAll('table.t3borderless tr')].filter(tr => tr.querySelector('input[type=radio]'));
    const entry = {
      nr,
      id,
      correct: rows.findIndex(tr => tr.querySelector('#FARBE')), // the site marks the correct radio button with id FARBE
      answers: rows.map(tr => tr.querySelector('td[headers=ANTWORT]').innerText.trim()),
    };
    if (!doc.querySelector('#P30_AUFGABENSTELLUNG_BILD img')) {
      entry.noImg = true;
      htmlTexts[nr] = doc.querySelector('#P30_AUFGABENSTELLUNG')?.innerText.trim();
    }
    if (entry.answers.length !== 4 || entry.correct < 0) console.warn(`question ${nr}: unexpected page`, entry);
    answers.push(entry);
    await new Promise(resolve => setTimeout(resolve, 250)); // be gentle with the server
  }

  frame.remove();
  window.bamfAnswers = answers; // also reachable when `copy` (a DevTools console helper) is not available
  if (typeof copy === 'function') copy(JSON.stringify(answers, null, 1));
  console.log(`${answers.length} questions captured (window.bamfAnswers). Plain-HTML question texts:`, htmlTexts);
})();
