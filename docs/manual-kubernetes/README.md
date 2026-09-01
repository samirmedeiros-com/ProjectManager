# Manual da Gestão Kubernetes

O PDF está em `docs/Manual-Gestao-Kubernetes.pdf`. Isto é a fonte, para o poder refazer quando
os ecrãs mudarem.

## Como refazer

Com a aplicação a correr localmente (`dotnet run` na API e `ng serve --configuration local` no
frontend), e com o `Kubernetes:Token` configurado — sem ele os ecrãs saem vazios:

```bash
# 1. capturar os ecrãs (usa o Playwright que o npx já tem em cache)
NODE_PATH=$(npm root -g)/../..$(: ) \
  node capturar.js          # escreve em img/

# 2. gerar o PDF
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
  --headless --disable-gpu --no-pdf-header-footer \
  --print-to-pdf=../Manual-Gestao-Kubernetes.pdf \
  "file://$PWD/manual.html"
```

O `capturar.js` entra com o administrador semeado, percorre os ecrãs e ajusta a altura da janela
ao conteúdo de cada um — sem isso cada captura traz uma faixa de fundo vazio que no PDF ocupa
meia folha. **Não executa nenhum comando sobre o cluster**: abre a confirmação de reinício só
para a fotografar e carrega em Cancelar.
