export interface SeurGuiaList {
  idt: number;
  guia?: string;
  referencia?: string;
  qtdVolumes?: number;
  contaDpd?: string;
  destinoNome?: string;
  destinoLocalidade?: string;
  destinoPais?: string;
  peso?: number;
  product?: string;
  service?: string;
  flagAtlas?: string;
  flagAtlasDescricao?: string;
  dtCriacao: string;
  dtAlteracao: string;
  shipmentCode?: string;
  codeStatusAtlas?: string;
  dtAtlas?: string;
}

export interface SeurGuiaDetail {
  idt: number;
  guia?: string;
  referencia?: string;
  qtdVolumes?: number;
  contaDpd?: string;
  peso?: number;
  valorCod?: number;
  // Origem
  origemNome?: string;
  origemMorada?: string;
  origemMoradaComplemento?: string;
  origemCodigoPostal?: string;
  origemLocalidade?: string;
  origemPais?: string;
  origemTelefone?: string;
  origemTelemovel?: string;
  origemFax?: string;
  origemEmail?: string;
  origemContatoNome?: string;
  origemContatoTelefone?: string;
  origemContatoTelemovel?: string;
  origemContatoEmail?: string;
  origemPraca?: string;
  // Destino
  destinoNome?: string;
  destinoMorada?: string;
  destinoMoradaComplemento?: string;
  destinoCodigoPostal?: string;
  destinoLocalidade?: string;
  destinoPais?: string;
  destinoTelefone?: string;
  destinoTelemovel?: string;
  destinoFax?: string;
  destinoEmail?: string;
  destinoContatoNome?: string;
  destinoContatoTelefone?: string;
  destinoContatoTelemovel?: string;
  destinoContatoEmail?: string;
  destinoPraca?: string;
  destino?: string;
  // Envio
  obs?: string;
  obsAdd?: string;
  produtoCodigo?: string;
  servicoCodigo?: string;
  product?: string;
  service?: string;
  cccc?: string;
  codCentro?: string;
  transportLine?: string;
  digito?: string;
  idRange?: number;
  range?: string;
  userId?: number;
  parcelNumber?: string;
  guiaDpd?: string;
  shipmentCode?: string;
  // Atlas
  flagAtlas?: string;
  flagAtlasDescricao?: string;
  codeStatusAtlas?: string;
  dtAtlas?: string;
  requestAtlas?: string;
  respAtlas?: string;
  // CIT
  flagCit?: string;
  dtCit?: string;
  respCit?: string;
  // Sistema
  flagAs400?: string;
  apagado?: string;
  // Verify
  verifyFlag?: string;
  verifyResp?: string;
  verifyInc?: string;
  verifyDatahoraInc?: string;
  verifyDatahoraUpd?: string;
  dataVerifyTrace?: string;
  verifyTrace?: string;
  // Datas sistema
  dtCriacao: string;
  dtAlteracao: string;
}

export interface SeurErro {
  idt: number;
  ecb?: string;
  referencia?: string;
  title?: string;
  status?: string;
  detail?: string;
  datahoraInsert?: string;
}

export interface SeurParcel {
  idt: number;
  ecbs?: string;
  parcelNumbers?: string;
  datahoraInsert?: string;
  flag?: string;
  shipmentCode?: string;
}

export interface SeurVerify {
  idt: number;
  guia?: string;
  inc?: string;
  dat?: string;
  hor?: string;
  datahoraInsert?: string;
  verifyFlag?: string;
  verifyFlagDescricao?: string;
  verifyResposta?: string;
  verifyData?: string;
}

export interface SeurTotais {
  total: number;
  enviado: number;
  naoEnviado: number;
  erro: number;
  outros: number;
}
