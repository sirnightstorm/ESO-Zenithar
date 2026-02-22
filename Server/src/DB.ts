import sqlite3 from "sqlite3"
import { open, Database } from "sqlite"

export default class DB {
    database: Database
    
    private constructor(database: Database) {
        this.database = database
    }

    static async open(path: string): Promise<DB> {
        console.log("Opening database " + path + "...")
        const database = await open({
            filename: path,
            driver: sqlite3.Database
            })
        console.log("Opening database " + path + " -  done")
        return new DB(database)
    }

    async createTables() {
        await this.database.exec(`
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT,
                rankIndex INTEGER
            );
            CREATE INDEX IF NOT EXISTS idx_users_name ON users (name);
            
            CREATE TABLE IF NOT EXISTS items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                itemLink TEXT,
                name TEXT,
                icon TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_items_itemLink ON items (itemLink);
            
            CREATE TABLE IF NOT EXISTS txns (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                eventId INTEGER,
                userId INTEGER,
                timeStamp INTEGER,
                qty INTEGER,
                itemId INTEGER,
                price INTEGER
            )
            `)
    }

    async beginTransaction() {
        await this.database.exec("BEGIN TRANSACTION")
    }

    async commitTransaction() {
        await this.database.exec("COMMIT TRANSACTION")
    }

    async insertUser(name: string, rankIndex: number): Promise<number> {
        await this.database.run(`
            INSERT OR IGNORE INTO users(name, rankIndex)
            VALUES (?, ?)`, name, rankIndex
        )

        return await this.getUserID(name)
    }

    async getUserID(name: string): Promise<number> {
        const result = await this.database.get("SELECT id FROM users WHERE name = ?", name)
        return result["id"] as number
    }


    async insertItem(itemLink: string, name: string, icon: string): Promise<number> {
        await this.database.run(`
            INSERT OR IGNORE INTO items(itemLink, name, icon)
            VALUES (?, ?, ?)`, itemLink, name, icon
        )

        return await this.getItemID(itemLink)
    }

    async getItemID(itemLink: string): Promise<number> {
        const result = await this.database.get("SELECT id FROM items WHERE itemLink = ?", itemLink)
        return result["id"] as number
    }


    async insertTxn(eventId: number, userId: number, timeStamp: number, qty: number, itemId: number, price: number) {
        await this.database.run(`
            INSERT OR IGNORE INTO txns(eventId, userId, timeStamp, qty, itemId, price)
            VALUES (?, ?, ?, ?, ?, ?)`, eventId, userId, timeStamp, qty, itemId, price
        )
    }


    async close() {
        this.database.close()
    }
}