export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SeurCpostal {
  idt: number;
  postcode: string;
  population?: string;
  country?: string;
  destFranchise?: string;
  plataform?: string;
  dtCriacao: string;
  dtAlteracao: string;
}

export interface SaveCpostalDto {
  postcode: string;
  population?: string;
  country?: string;
  destFranchise?: string;
  plataform?: string;
}

export interface SeurDestino {
  idt: number;
  destinoCode: string;
  plataformCode?: string;
  productCode?: string;
  serviceCode?: string;
  destination?: string;
  loadLine?: string;
  transportLine?: string;
  dtCriacao: string;
  dtAlteracao: string;
}

export interface SaveDestinoDto {
  destinoCode: string;
  plataformCode?: string;
  productCode?: string;
  serviceCode?: string;
  destination?: string;
  loadLine?: string;
  transportLine?: string;
}

export interface SeurProduct {
  idt: number;
  productCode: string;
  product?: string;
  shortName?: string;
  dtCriacao: string;
  dtAlteracao: string;
}

export interface SaveProductDto {
  productCode: string;
  product?: string;
  shortName?: string;
}

export interface SeurService {
  idt: number;
  serviceCode: string;
  service?: string;
  shortName?: string;
  dtCriacao: string;
  dtAlteracao: string;
}

export interface SaveServiceDto {
  serviceCode: string;
  service?: string;
  shortName?: string;
}

export interface CwentNum {
  account: string;
  bicNumber: number;
  shEmail2Dest?: string;
  shReady2Collect?: string;
  cp4Allow?: string;
  serviceRcSeqNumber?: string;
  agregateBilling?: string;
  grAssinada?: string;
  weigth0Allow?: string;
  labelType?: string;
  serviceB2c?: string;
  servFrio?: string;
  servAux?: string;
  descAux?: string;
  bicsUser?: string;
  bicsPwd?: string;
  bicsCi?: string;
  bicsNif?: string;
  bicsCcc?: string;
  bicsPraca?: string;
  bicsCcc2?: string;
  bicsUsr2?: string;
  bicsServ?: string;
  bicsProd?: string;
  bicvPortal?: string;
  bicAux1?: string;
  bicAux2?: string;
  bicAux3?: string;
  bicAux4?: string;
}

export interface SaveCwentNumDto {
  bicNumber: number;
  shEmail2Dest?: string;
  shReady2Collect?: string;
  cp4Allow?: string;
  serviceRcSeqNumber?: string;
  agregateBilling?: string;
  grAssinada?: string;
  weigth0Allow?: string;
  labelType?: string;
  serviceB2c?: string;
  servFrio?: string;
  servAux?: string;
  descAux?: string;
  bicsUser?: string;
  bicsPwd?: string;
  bicsCi?: string;
  bicsNif?: string;
  bicsCcc?: string;
  bicsPraca?: string;
  bicsCcc2?: string;
  bicsUsr2?: string;
  bicsServ?: string;
  bicsProd?: string;
  bicvPortal?: string;
  bicAux1?: string;
  bicAux2?: string;
  bicAux3?: string;
  bicAux4?: string;
}

export interface CreateCwentNumDto extends SaveCwentNumDto {
  account: string;
}
