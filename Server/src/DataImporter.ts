import { parseZenitharData, LuaTable } from "./ZenitharDataParser";
import DB from "./DB"

export default class DataWriter {

    db: DB
    userMap = new Map<number, number>
    itemMap = new Map<number, number>

    constructor(db: DB) {
        this.db = db
    }

    async import(lua: string, guildId: string): Promise<boolean> {

        const zenitharData: LuaTable = parseZenitharData(lua)
        var changed = false

        const defaultObject = zenitharData["Default"] as LuaTable
        for (const [accountName, accountData] of Object.entries(defaultObject)) {
            console.log(`Importing account ${accountName}`)
            const notProcessed = (accountData["$AccountWide"]["processed"] === 0)
            const hasTxns = await this.importGuildData(accountData["$AccountWide"]["guilds"][guildId])
            changed ||= (notProcessed && hasTxns)
        }

        return changed
    }

    private async importGuildData(guildData: object): Promise<boolean> {
        console.log(" - Importing users...")
        await this.importUsers(guildData["users"] as object)
        console.log(" - Importing items...")
        await this.importItems(guildData["items"])
        console.log(" - Importing transactions...")
        await this.importTransactions(guildData["txns"])

        return Object.keys(guildData["txns"]).length > 0
    }

    private async importUsers(users: object) {
        await this.db.beginTransaction()
        for (const [name, userData] of Object.entries(users)) {
            const luaUserId = userData.id
            const dbUserId = await this.db.insertUser(name, userData.rankIndex)
            this.userMap.set(luaUserId, dbUserId)
        }
        await this.db.commitTransaction()
    }

    private async importItems(items: object) {
        await this.db.beginTransaction()
        for (const [itemLink, itemData] of Object.entries(items)) {
            const luaItemId = itemData.id
            const dbItemId = await this.db.insertItem(itemLink, itemData.name, itemData.icon)
            this.itemMap.set(luaItemId, dbItemId)
        }
        await this.db.commitTransaction()
    }

    private async importTransactions(txns: object) {
        await this.db.beginTransaction()
        for (const [eventId, txnString] of Object.entries(txns)) {
            const [luaUserId, timeStamp, qty, luaItemId, price] = (txnString as string).split("~")
            const dbUserId = this.userMap.get(Number(luaUserId))
            const dbItemId = this.itemMap.get(Number(luaItemId))
            await this.db.insertTxn(Number(eventId), dbUserId, Number(timeStamp), Number(qty), dbItemId, Number(price))
        }
        await this.db.commitTransaction()
    }
}
