import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OraConsoleService } from '../../services/oraconsole.service';
import { OraConsoleAuthService } from '../../services/oraconsole-auth.service';
import { OraConsoleColumn, OraConsoleQueryResult, OraConsoleSchema, OraConsoleTable } from '../../models/oraconsole.model';

const ROWID_COLUMN = 'ORACONSOLE_ROWID';

@Component({
  selector: 'app-oraconsole-workbench',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './oraconsole-workbench.component.html',
  styleUrls: ['./oraconsole-workbench.component.css']
})
export class OraConsoleWorkbenchComponent implements OnInit {
  username: string | null = null;

  schemas: OraConsoleSchema[] = [];
  loadingSchemas = false;
  schemaError = '';

  expandedSchemas = new Set<string>();
  tablesBySchema: Record<string, OraConsoleTable[]> = {};
  loadingTablesFor: string | null = null;

  expandedTables = new Set<string>();
  columnsByTable: Record<string, OraConsoleColumn[]> = {};
  loadingColumnsFor: string | null = null;

  sql = '';
  executing = false;
  queryError = '';
  result: OraConsoleQueryResult | null = null;
  pageSize = 100;

  private lastGeneratedSql: string | null = null;
  editableTarget: { owner: string; tableName: string } | null = null;
  editingCell: { rowIndex: number; column: string } | null = null;
  editingValue = '';
  savingCell = false;

  constructor(
    private oraConsoleService: OraConsoleService,
    private oraAuthService: OraConsoleAuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.username = this.oraAuthService.currentUsernameValue;
    this.loadSchemas();
  }

  loadSchemas() {
    this.loadingSchemas = true;
    this.schemaError = '';
    this.oraConsoleService.getSchemas().subscribe({
      next: (schemas) => {
        this.schemas = schemas;
        this.loadingSchemas = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.schemaError = err?.error?.message || 'Erro ao carregar schemas';
        this.loadingSchemas = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleSchema(owner: string) {
    if (this.expandedSchemas.has(owner)) {
      this.expandedSchemas.delete(owner);
      return;
    }
    this.expandedSchemas.add(owner);
    if (!this.tablesBySchema[owner]) {
      this.loadingTablesFor = owner;
      this.oraConsoleService.getTables(owner).subscribe({
        next: (tables) => {
          this.tablesBySchema[owner] = tables;
          this.loadingTablesFor = null;
          this.cdr.detectChanges();
        },
        error: () => {
          this.tablesBySchema[owner] = [];
          this.loadingTablesFor = null;
          this.cdr.detectChanges();
        }
      });
    }
  }

  toggleTable(owner: string, tableName: string) {
    const key = `${owner}.${tableName}`;
    if (this.expandedTables.has(key)) {
      this.expandedTables.delete(key);
      return;
    }
    this.expandedTables.add(key);
    if (!this.columnsByTable[key]) {
      this.loadingColumnsFor = key;
      this.oraConsoleService.getColumns(owner, tableName).subscribe({
        next: (columns) => {
          this.columnsByTable[key] = columns;
          this.loadingColumnsFor = null;
          this.cdr.detectChanges();
        },
        error: () => {
          this.columnsByTable[key] = [];
          this.loadingColumnsFor = null;
          this.cdr.detectChanges();
        }
      });
    }
  }

  insertSelect(owner: string, tableName: string) {
    const sql = `SELECT t.*, t.ROWID AS ${ROWID_COLUMN} FROM "${owner}"."${tableName}" t`;
    this.sql = sql;
    this.lastGeneratedSql = sql;
    this.editableTarget = { owner, tableName };
    this.execute(1);
  }

  execute(page: number = 1) {
    if (!this.sql.trim()) return;
    if (this.sql !== this.lastGeneratedSql) {
      this.editableTarget = null;
    }
    this.editingCell = null;
    this.executing = true;
    this.queryError = '';
    this.oraConsoleService.execute(this.sql, page, this.pageSize).subscribe({
      next: (result) => {
        this.result = result;
        this.executing = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.queryError = err?.error?.message || 'Erro ao executar a instrução';
        this.result = null;
        this.executing = false;
        this.cdr.detectChanges();
      }
    });
  }

  nextPage() {
    if (this.result?.hasMore) {
      this.execute(this.result.page + 1);
    }
  }

  prevPage() {
    if (this.result && this.result.page > 1) {
      this.execute(this.result.page - 1);
    }
  }

  get resultColumns(): string[] {
    return (this.result?.columns ?? []).filter((c) => c !== ROWID_COLUMN);
  }

  formatCell(value: any): string {
    if (value === null || value === undefined) return 'NULL';
    return String(value);
  }

  isEditingCell(rowIndex: number, column: string): boolean {
    return this.editingCell?.rowIndex === rowIndex && this.editingCell?.column === column;
  }

  startCellEdit(rowIndex: number, column: string, currentValue: any) {
    if (!this.editableTarget || this.savingCell) return;
    this.editingCell = { rowIndex, column };
    this.editingValue = currentValue === null || currentValue === undefined ? '' : String(currentValue);
  }

  cancelCellEdit() {
    this.editingCell = null;
  }

  saveCellEdit() {
    if (!this.editingCell || !this.editableTarget || !this.result) {
      this.editingCell = null;
      return;
    }

    const { rowIndex, column } = this.editingCell;
    const row = this.result.rows[rowIndex];
    const rowId = row[ROWID_COLUMN];
    const previousValue = row[column];
    const newValue: string | null = this.editingValue === '' ? null : this.editingValue;

    if (newValue === previousValue || (newValue === null && previousValue === null)) {
      this.editingCell = null;
      return;
    }

    this.savingCell = true;
    this.oraConsoleService.updateCell({
      owner: this.editableTarget.owner,
      tableName: this.editableTarget.tableName,
      columnName: column,
      rowId,
      value: newValue
    }).subscribe({
      next: () => {
        row[column] = newValue;
        this.editingCell = null;
        this.savingCell = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.queryError = err?.error?.message || 'Erro ao guardar a alteração';
        this.editingCell = null;
        this.savingCell = false;
        this.cdr.detectChanges();
      }
    });
  }

  logout() {
    this.oraAuthService.logout();
    this.router.navigate(['/login-oraconsole']);
  }

  goBack() {
    this.router.navigate(['/portal']);
  }
}
