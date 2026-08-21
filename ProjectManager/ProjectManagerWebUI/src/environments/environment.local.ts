// Usado só por `ng serve --configuration local`: aponta para a API a correr na
// própria máquina, em vez do servidor 10.2.2.93 do environment.ts.
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5001',
  seurApiUrl: 'http://localhost:5001'
};
