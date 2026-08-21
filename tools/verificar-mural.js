// Verifica se o mural de TV está a cortar ou a sobrepor conteúdo, em várias
// resoluções — da TV ao portátil e à janela estreita.
//
// Os cards têm `overflow: hidden`, por isso o que não cabe desaparece sem aviso.
// A olho nu, numa janela só, não se vê o que vai acontecer nas outras.
//
// Deteta quatro coisas, e as quatro já aconteceram neste projeto:
//   - conteúdo que sai por baixo    (cards demasiado cheios)
//   - conteúdo que sai pela direita (tabelas que não encolhem — corte horizontal,
//     invisível para quem só verifique a altura)
//   - texto TRUNCADO com reticências ou line-clamp, que não sai da caixa mas
//     também não se lê; foi o último a ser apanhado e o mais difícil de ver
//   - cards sobrepostos
//
//   npm install playwright
//   node tools/verificar-mural.js "http://localhost:4200/tv?k=<chave>"
//
// VARRER=1 percorre larguras de 380 a 1920 em passos de 80, em quatro alturas —
// útil para não deixar buracos entre os tamanhos da lista fixa.
//
// Correr sempre depois de mexer em tv-cards.ts ou no SCSS.

const { chromium } = require('playwright');
const TAM = process.env.VARRER
  ? (() => { const t=[]; for (let w=380; w<=1920; w+=80) for (const h of [620,760,900,1080]) t.push([w,h]); return t; })()
  : [[1920,1080],[1512,982],[1440,900],[1366,768],[1280,700],[1100,700],[1024,640],[900,600],[768,900],[600,800],[420,760]];
(async () => {
  const b = await chromium.launch({ executablePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome' });
  for (const [w,h] of TAM) {
    const p = await b.newPage({ viewport: { width: w, height: h } });
    await p.goto(process.argv[2], { waitUntil: 'networkidle' });
    await p.waitForTimeout(1800);
    const r = await p.evaluate(() => {
      const probs = [];
      // conteúdo que sai da caixa do seu card (o card tem overflow hidden)
      document.querySelectorAll('.card').forEach((c) => {
        const cb = c.getBoundingClientRect();
        const t = c.querySelector('.card-titulo')?.textContent?.trim();
        if (c.scrollHeight - c.clientHeight > 2) probs.push(`${t}: transborda ${c.scrollHeight-c.clientHeight}px`);
        c.querySelectorAll('*').forEach((e) => {
          if (!e.textContent?.trim()) return;
          const eb = e.getBoundingClientRect();
          if (eb.height === 0) return;
          if (eb.bottom - cb.bottom > 1) probs.push(`${t}: "${e.textContent.trim().slice(0,20)}" sai por baixo`);
          if (eb.right - cb.right > 1) probs.push(`${t}: "${e.textContent.trim().slice(0,20)}" sai pela direita`);
          // Texto truncado com reticências ou line-clamp: não sai da caixa, mas
          // também não se lê. É preciso comparar o conteúdo com a caixa dele.
          if (e.children.length === 0) {
            if (e.scrollWidth > e.clientWidth + 1) probs.push(`${t}: TRUNCADO na largura -> "${e.textContent.trim().slice(0,28)}"`);
            if (e.scrollHeight > e.clientHeight + 1) probs.push(`${t}: TRUNCADO na altura -> "${e.textContent.trim().slice(0,28)}"`);
          }
        });
      });
      // cards sobrepostos
      const cards = [...document.querySelectorAll('.card')].map(c => c.getBoundingClientRect());
      for (let i=0;i<cards.length;i++) for (let j=i+1;j<cards.length;j++) {
        const a=cards[i],bb=cards[j];
        if (a.left < bb.right-1 && bb.left < a.right-1 && a.top < bb.bottom-1 && bb.top < a.bottom-1) probs.push('CARDS SOBREPOSTOS');
      }
      return { probs: [...new Set(probs)], scrollX: document.documentElement.scrollWidth > window.innerWidth };
    });
    const scroll = r.scrollX ? ' [SCROLL HORIZONTAL]' : '';
    console.log(`${String(w).padStart(4)}x${String(h).padStart(4)}: ${r.probs.length ? '\n   ' + r.probs.slice(0,5).join('\n   ') : 'ok'}${scroll}`);
    await p.close();
  }
  await b.close();
})();
