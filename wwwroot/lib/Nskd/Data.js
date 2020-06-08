if ((typeof Nskd) === 'undefined') Nskd = {};

Nskd.Data = (Nskd.Data) ? Nskd.Data : {};

// добавляет функциональность к объекту DataSet
// возвращает { __type: String, tables: [], relations: [] }
Nskd.Data.DataSet = function (dataSet) {
    if (!Nskd.Js.is(dataSet, 'object') || dataSet.__type != 'Nskd.Data.DataSet') { return null; }
    var tables = [];
    for (var ti = 0; ti < dataSet.tables.length; ti++) {
        var table = Nskd.Data.DataTable(dataSet.tables[ti]);
        tables.push(table);
    }
    return {
        __type: dataSet.__type,
        tables: tables,
        relations: dataSet.relations
    };
};

// добавляет функциональность к объекту DataTable
// возвращает { __type: String, tableName: String, columns: [], primaryKey: [], extendedProperties: {}, rows: [], + functions }
Nskd.Data.DataTable = function (dataTable) {
    if (!Nskd.Js.is(dataTable, 'object') || dataTable.__type != 'Nskd.Data.DataTable') { return null; }
    var rows = [];
    for (var ri = 0; ri < dataTable.rows.length; ri++) {
        var row = Nskd.Data.DataRow(dataTable.rows[ri]);
        rows.push(row);
    }
    return {
        __type: dataTable.__type,
        tableName: dataTable.tableName,
        columns: dataTable.columns,
        primaryKey: dataTable.primaryKey,
        extendedProperties: dataTable.extendedProperties,
        rows: rows,
        gci: function (columnName) {
            var columnIndex = null;
            var cols = this.columns;
            if (cols) {
                for (var ci = 0; ci < cols.length; ci++) {
                    var col = cols[ci];
                    if (col && (col.columnName == columnName)) {
                        columnIndex = ci;
                        break;
                    }
                }
            }
            return columnIndex;
        },
        getRowByKey: function (key, keyColIndex) {
            var row = null;
            if (!keyColIndex) keyColIndex = 0;
            for (var ri = 0; ri < this.rows.length; ri++) {
                row = this.rows[ri];
                if (Nskd.Js.is(key, 'array')) {
                    var ok = true;
                    for (var ki = 0; ki < key.length; ki++) {
                        if (row.itemArray[keyColIndex][ki] != key[ki]) {
                            ok = false;
                            break;
                        }
                    }
                    if (ok) break;
                } else {
                    if (row.itemArray[keyColIndex] == key) {
                        break;
                    }
                }
            }
            if (ri == this.rows.length) row = null;
            return row;
        }
    };
};

// добавляет функциональность к объекту DataRow
// возвращает { itemArray: [], rowState: string }.
Nskd.Data.DataRow = function (dataRow) {
    // возможно два варианта:
    // 1 - dataRow = []. 
    // 2 - dataRow = { itemArray: [], rowState: string }.
    var row; // возвращаемый объект
    if (Nskd.Js.is(dataRow, 'array')) {
        // вариант до 2016-10-25 - переделываем его на новый. 
        row = { 'itemArray': dataRow, 'rowState': 'Unchanged' };
    } else if (Nskd.Js.is(dataRow, 'object') &&
        Nskd.Js.is(dataRow.itemArray, 'array') &&
        Nskd.Js.is(dataRow.rowState, 'string')) {
        // новый вариант - оставляем как есть.
        row = dataRow;
    } else { 
        // не то не сё
        row = null;
    }
    return row;
};
