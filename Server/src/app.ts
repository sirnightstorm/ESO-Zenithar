import * as fs from "fs";
import os from "os";
import path from "path";
import psList from 'ps-list'
import { JSONFilePreset } from 'lowdb/node' // Currently unused

import DB from "./DB"
import DataImporter from "./DataImporter"
import WebServer from "./WebServer"
import SavedVarsWatcher from "./SavedVarsWatcher"

const home = os.homedir();
const svDir = path.join(home, "Documents", "Elder Scrolls Online", "live", "SavedVariables");
const svFilePath = path.join(svDir, "Zenithar.lua");

global.rootPath = path.join(__dirname, "..")

DB.path = path.join(global.rootPath, "assets", "Zenithar.sqlite")

const watcher = new SavedVarsWatcher(svFilePath, importSavedVars)

// const watcher = chokidar.watch(svFilePath, {
//     // ignored: (path, stats) => stats?.isFile() && !path.endsWith('.js'), // only watch js files
//     persistent: true,
//     awaitWriteFinish: true
// })

// const app = express()
// const port = 8001
const ws = new WebServer()

process.on("beforeExit", async () => {
    await watcher.stop()
    process.exit(0) // if you don't close yourself this will run forever
});


// app.set("root", path.resolve(path.join(__dirname, "..")));

// app.engine('handlebars', engine());
// app.set('view engine', 'handlebars');
// app.set('views', path.join(__dirname, "..", "views"))

// app.get('/', (req, res) => {
//     res.render('home', {
//         title: "Home",
//     });
// });
// app.get("/", (req, res) => {
//   res.send("Hello World!");
// });

// app.use(express.static(__dirname + "/node_modules/bootstrap/dist"))

//  app.listen(port, () => {
//    return console.log(`Express is listening at http://localhost:${port}`);
//  });
ws.listen()

async function importSavedVars(svFilePath: string) {
    global.lowdb = await JSONFilePreset(path.join(global.rootPath, "store.json"), {})

    console.log("Opening database for import")
    const db = await DB.open()

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
    const db = await DB.open()
    await db.createTables()
    await db.close()

    // Initial import
    console.log("Initial import")
    await importSavedVars(svFilePath)

    watcher.start()
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