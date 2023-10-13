def create_table():
    return [0, 0, 0, 0]


def add_to_table(square, value, table):
    if value % 5 != 0:
        raise Exception(str(value) + " is not a multiple of 5!")
    if value > 40 or value < -35:
        raise Exception(str(value) + " is not within a the valid range [-35,40]!")

    table[int(square / 16)] = table[int(square / 16)] | (int(value / 5) + 7) << ((square % 16) * 4)


def convert_table_array(array, table=None):
    if table is None:
        table = create_table()
    for idx, value in enumerate(array):
        add_to_table(idx, value, table)
    return table


pawnPieceTable = [0,   0,   0,   0,   0,   0,   0,   0,
                  5,   10,  10, -20, -20,  10,  10,  5,
                  5,  -5,  -10,  0,   0,  -10, -5,   5,
                  0,   0,   0,   20,  20,  0,   0,   0,
                  5,   5,   10,  25,  25,  10,  5,   5,
                  10,  10,  20,  30,  30,  20,  10,  10,
                  40,  40,  40,  40,  40,  40,  40,  40,
                  0,   0,   0,   0,   0,   0,   0,   0, ]

knightPieceTable = [-35, -35, -30, -30, -30, -30, -35, -35,
                    -35, -20, 0, 5, 5, 0, -20, -35,
                    -30, 5, 20, 25, 25, 20, 5, -30,
                    -30, 0, 25, 30, 30, 25, 0, -30,
                    -30, 5, 25, 30, 30, 25, 5, -30,
                    -30, 0, 20, 25, 25, 20, 0, -30,
                    -35, -20, 0, 0, 0, 0, -20, -35,
                    -35, -35, -30, -30, -30, -30, -35, -35, ]

bishopPieceTable = [-20, -10, -10, -10, -10, -10, -10, -20, 
                    -10,  5,  0,  0,  0,  0,  5, -10,
                    -10, 10, 10, 10, 10, 10, 10, -10,
                    -10,  0, 10, 10, 10, 10,  0, -10,
                    -10,  5,  5, 10, 10,  5,  5, -10,
                    -10,  0,  5, 10, 10,  5,  0, -10,
                    -10,  0,  0,  0,  0,  0,  0, -10,
                    -20, -10, -10, -10, -10, -10, -10, -20, ]

rookPieceTable = [0,  0,  0,  5,  5,  0,  0,  0,
                  -5,  0,  0,  0,  0,  0,  0, -5,
                  -5,  0,  0,  0,  0,  0,  0, -5,
                  -5,  0,  0,  0,  0,  0,  0, -5,
                  -5,  0,  0,  0,  0,  0,  0, -5,
                  -5,  0,  0,  0,  0,  0,  0, -5,
                  5, 10, 10, 10, 10, 10, 10,  5,
                  0,  0,  0,  0,  0,  0,  0,  0, ]

queenPieceTable = [-20,-10,-10, -5, -5,-10,-10,-20,
                   -10,  0,  5,  0,  0,  0,  0,-10,
                   -10,  5,  5,  5,  5,  5,  0,-10,
                   0,  0,  5,  5,  5,  5,  0, -5,
                   -5,  0,  5,  5,  5,  5,  0, -5,
                   -10,  0,  5,  5,  5,  5,  0,-10,
                   -10,  0,  0,  0,  0,  0,  0,-10,
                   -20,-10,-10, -5, -5,-10,-10,-20,]

kingMidPieceTable = [20, 30, 10,  0,  0, 10, 30, 20,
                     20, 20,  0,  0,  0,  0, 20, 20,
                     -10,-20,-20,-20,-20,-20,-20,-10,
                     -20,-30,-30,-35,-35,-30,-30,-20,
                     -30,-35,-35,-35,-35,-35,-35,-30,
                     -30,-35,-35,-35,-35,-35,-35,-30,
                     -30,-35,-35,-35,-35,-35,-35,-30,
                     -30,-35,-35,-35,-35,-35,-35,-30,]

kingEndPieceTable = [-35,-30,-30,-30,-30,-30,-30,-35,
                     -30,-30,  0,  0,  0,  0,-30,-30,
                     -30,-10, 20, 30, 30, 20,-10,-30,
                     -30,-10, 30, 40, 40, 30,-10,-30,
                     -30,-10, 30, 40, 40, 30,-10,-30,
                     -30,-10, 20, 30, 30, 20,-10,-30,
                     -30,-20,-10,  0,  0,-10,-20,-30,
                     -35,-35,-30,-20,-20,-30,-35,-35,]
allTables = [pawnPieceTable, knightPieceTable, bishopPieceTable, rookPieceTable, queenPieceTable, kingMidPieceTable, kingEndPieceTable]

allConvertedTables = []

for table in allTables:
    convertedTable = convert_table_array(table)
    for partialTable in convertedTable:
        allConvertedTables.append(partialTable)
print(list(map(bin, allConvertedTables)))
print(allConvertedTables)
searchTable = 0
square = 11
print((((allConvertedTables[(searchTable * 4) + int(square/16)] >> (square % 16)*4) & 15) -7 ) *5)
