import * as fs from "fs";
// import { readFile, writeFile } from "fs/promises"
import os from "os";
import path from "path";
// import psList from 'ps-list'
import { JSONFilePreset } from 'lowdb/node' // Currently unused

import DB from "./DB"
import DataImporter from "./DataImporter"
import WebServer from "./WebServer"
import SavedVarsWatcher from "./SavedVarsWatcher"
import ProcessedValueWriter from "./ProcessedValueWriter";

const home = os.homedir();
const svDir = path.join(home, "Documents", "Elder Scrolls Online", "live", "SavedVariables");
const svFilePath = path.join(svDir, "Zenithar.lua");

global.rootPath = path.join(__dirname, "..")

DB.path = path.join(global.rootPath, "assets", "Zenithar.sqlite")

const watcher = new SavedVarsWatcher(svFilePath, importSavedVars)

const processedValueWriter = new ProcessedValueWriter(svFilePath)

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

// async function isESORunning(): Promise<boolean> {
//   const processes = await psList();
//   return processes.some(p => p.name.toLowerCase() === "eso64.exe");
// }

// async function markProcessed(filePath: string): Promise<void> {
//     // Read file as UTF‑8 text
//     let content = await readFile(filePath, "utf8");

//     // Replace only the exact match
//     const updated = content.replace('["processed"] = 0', '["processed"] = 1');

//     // Write back only if something changed
//     if (updated !== content) {
//         console.log("Marking data as processed")
//         await writeFile(filePath, updated, "utf8");
//     }
// }

// var watcherInterval: NodeJS.Timeout = null

// function watchForESOExit(filePath: string): void {
//     if (watcherInterval != null) {
//         return // Watcher already running
//     }

//     console.log("Waiting for ESO to close...");
//     watcherInterval = setInterval(async () => {
//         try {
//             const running = await isESORunning();

//             if (!running) {
//                 clearInterval(watcherInterval);   // stop checking
//                 await markProcessed(filePath);
//                 console.log("ESO closed — processed flag updated");
//                 watcherInterval = null
//             }
//         } catch (err) {
//             console.error("Error checking ESO status:", err);
//         }
//     }, 10_000); // 10 seconds
// }

async function importSavedVars(svFilePath: string) {
    global.lowdb = await JSONFilePreset(path.join(global.rootPath, "store.json"), {})

    console.log("Opening database for import")
    const db = await DB.open()

    console.log("About to read '" + svFilePath + "'")
    const lua = fs.readFileSync(svFilePath, "utf8");

    const importer = new DataImporter(db)
    console.log("About to write to database...")
    const changed = await importer.import(lua, "767808")

    await db.close()
    console.log("Database writes finished")

    if (changed) {
        processedValueWriter.run()
    }
}

async function main() {
    console.log("Initialising database.")
    const db = await DB.open()
    await db.createTables()
    await db.close()

    watcher.start()

    ws.listen()
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