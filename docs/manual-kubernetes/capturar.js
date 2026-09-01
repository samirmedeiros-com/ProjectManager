const { chromium } = require('playwright');

const BASE = 'http://localhost:4200';
const DIR = process.env.SP + '/manual/img';

const esperar = (ms) => new Promise((r) => setTimeout(r, ms));

/**
 * Ajusta a altura da janela ao conteúdo. Sem isto cada captura de página traz uma faixa
 * grande de fundo vazio por baixo da tabela, que no PDF fica a ocupar meia folha.
 */
async function ajustar(pagina, seletor) {
  const altura = await pagina.evaluate((s) => {
    const el = document.querySelector(s);
    return el ? Math.ceil(el.getBoundingClientRect().bottom) : 0;
  }, seletor);

  await pagina.setViewportSize({
    width: 1440,
    height: Math.min(1400, Math.max(420, altura + 32)),
  });
  await esperar(400);
}

async function tirar(pagina, nome, opcoes = {}) {
  await esperar(opcoes.pausa ?? 600);
  await pagina.screenshot({ path: `${DIR}/${nome}.png`, ...opcoes.shot });
  console.log('  ✓', nome);
}

(async () => {
  const browser = await chromium.launch();
  const contexto = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2,
    locale: 'pt-PT',
  });
  const p = await contexto.newPage();

  console.log('portal e login');
  await p.goto(`${BASE}/portal`, { waitUntil: 'networkidle' });
  await ajustar(p, '.portal-container');
  await tirar(p, '01-portal');

  await p.click('text=Gestão Kubernetes');
  await p.waitForURL('**/login-kubernetes');
  await ajustar(p, '.login-box');
  await tirar(p, '02-login');

  await p.fill('input[type=email]', 'admin@kubernetes.local');
  await p.fill('input[type=password]', 'k8s2026');
  await p.click('button[type=submit]');
  await p.waitForURL('**/kubernetes', { timeout: 20000 });
  await p.waitForSelector('table.tabela tbody tr', { timeout: 20000 });

  console.log('lista de deployments');
  await ajustar(p, '.k8s');
  await tirar(p, '03-cluster', { pausa: 1500 });

  // O namespace qualidade tem um único deployment: as capturas dos detalhes ficam legíveis.
  await p.click('button.aba:has-text("qualidade")');
  await p.waitForSelector('table.tabela tbody tr', { timeout: 20000 });
  await ajustar(p, '.k8s');
  await tirar(p, '04-namespace', { pausa: 1200 });

  console.log('pods');
  await p.click('button.btn-expandir');
  await p.waitForSelector('table.tabela-pods tbody tr', { timeout: 20000 });
  await ajustar(p, '.k8s');
  await tirar(p, '05-pods', { pausa: 1200 });

  console.log('consola do pod');
  await p.setViewportSize({ width: 1440, height: 900 });
  await p.click('table.tabela-pods button:has-text("Ver")');
  await p.waitForSelector('.log-caixa .log-linha', { timeout: 25000 });
  await tirar(p, '06-log', { pausa: 2500 });
  await p.click('.log-cabecalho button:has-text("Fechar")');
  await esperar(400);

  console.log('informação do deployment');
  await p.click('button.btn-info');
  await p.waitForSelector('.modal-nota #nota-titulo', { timeout: 15000 });
  await tirar(p, '07-info', { pausa: 1200 });
  await p.click('.modal-nota button:has-text("Cancelar")');
  await esperar(400);

  console.log('confirmação de comando');
  await p.click('button.btn-acao:has-text("Reiniciar")');
  await p.waitForSelector('.modal h2', { timeout: 10000 });
  await tirar(p, '08-confirmar', { pausa: 800 });
  await p.click('.modal button:has-text("Cancelar")');
  await esperar(400);

  console.log('registo do deployment');
  await p.click('button.btn-acao--registo');
  await p.waitForSelector('.modal-registo', { timeout: 15000 });
  await tirar(p, '09-registo-deployment', { pausa: 2000 });
  await p.click('.modal-registo button:has-text("Fechar")');
  await esperar(400);

  console.log('registo global');
  await p.click('nav.menu a:has-text("Registo de ações")');
  await p.waitForURL('**/kubernetes/registo');
  await p.waitForSelector('table.tabela tbody tr', { timeout: 20000 });
  await ajustar(p, '.pagina');
  await tirar(p, '10-registo-global', { pausa: 1500 });

  // Uma linha com o antes/depois aberto vale mais do que a explicação escrita.
  const verAlteracao = p.locator('button.ver-alteracao').first();
  if (await verAlteracao.count()) {
    await verAlteracao.click();
    await ajustar(p, '.pagina');
    await tirar(p, '11-antes-depois', { pausa: 900 });
  }

  console.log('utilizadores');
  await p.click('nav.menu a:has-text("Utilizadores")');
  await p.waitForURL('**/kubernetes/utilizadores');
  await p.waitForSelector('table.tabela tbody tr', { timeout: 20000 });
  await ajustar(p, '.pagina');
  await tirar(p, '12-utilizadores', { pausa: 1200 });

  await p.click('button:has-text("Novo utilizador")');
  await p.waitForSelector('form.formulario', { timeout: 10000 });
  await p.fill('input[name=nome]', 'Maria Silva');
  await p.fill('input[name=email]', 'maria.silva@dpd.pt');
  await p.selectOption('select[name=papel]', 'Operador');
  await ajustar(p, '.pagina');
  await tirar(p, '13-novo-utilizador', { pausa: 800 });

  await browser.close();
  console.log('feito');
})().catch((e) => { console.error('FALHOU:', e.message); process.exit(1); });
