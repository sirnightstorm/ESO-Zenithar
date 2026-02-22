import express from "express";
import * as fs from "fs";
import os from "os";
import path from "path";
import chokidar, { FSWatcher } from "chokidar"
import DB from "./DB"
import DataImporter from "./DataImporter";

const home = os.homedir();
const svDir = path.join(home, "Documents", "Elder Scrolls Online", "live", "SavedVariables");
const svFilePath = path.join(svDir, "Zenithar.lua");

const dbFilePath = path.join(__dirname, "..", "assets", "Zenithar.sqlite")

const watcher = chokidar.watch(svFilePath, {
    // ignored: (path, stats) => stats?.isFile() && !path.endsWith('.js'), // only watch js files
    persistent: true,
    awaitWriteFinish: true
})

const app = express()
// const port = 3000

process.on("beforeExit", async () => {
    await watcher.close()
    process.exit(0) // if you don't close yourself this will run forever
});

app.get("/", (req, res) => {
  res.send("Hello World!");
});

// app.listen(port, () => {
//   return console.log(`Express is listening at http://localhost:${port}`);
// });

async function importSavedVars() {
    console.log("Opening database for import")
    const db = await DB.open(dbFilePath)

    console.log("About to read '" + svFilePath + "'")
    const lua = fs.readFileSync(svFilePath, "utf8");

    const importer = new DataImporter(db)
    console.log("About to write to database...")
    await importer.import(lua, "767808")

    await db.close()
    console.log("Database writes finished")
}

async function main() {
    console.log("Initialising database.")
    const db = await DB.open(dbFilePath)
    await db.createTables()
    await db.close()

    // Initial import
    console.log("Initial import")
    await importSavedVars()

    watcher
        .on("add", async (path) => {
            console.log(`File ${path} has been added`)
            await importSavedVars()
        })
        .on("change", async (path) => {
            console.log(`File ${path} has been changed`)
            await importSavedVars()
        })
}


(async () => {
    try {
        await main()
    } catch (e) {
        // Deal with the fact the chain failed
        console.log("Failed main: " + e)
    }
    // `text` is not available here
})();