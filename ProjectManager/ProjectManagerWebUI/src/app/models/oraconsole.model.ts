export interface OraConsoleLoginRequest {
  username: string;
  password: string;
}

export interface OraConsoleLoginResponse {
  success: boolean;
  message?: string;
  token?: string;
  username?: string;
}

export interface OraConsoleSchema {
  owner: string;
}

export interface OraConsoleTable {
  tableName: string;
}

export interface OraConsoleColumn {
  columnName: string;
  dataType: string;
  dataLength?: number;
  nullable: string;
  dataDefault?: string;
  columnId: number;
}

export interface OraConsoleQueryResult {
  isResultSet: boolean;
  columns: string[];
  rows: Record<string, any>[];
  affectedRows?: number;
  elapsedMs: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface OraConsoleCellUpdateRequest {
  owner: string;
  tableName: string;
  columnName: string;
  rowId: string;
  value: string | null;
}
