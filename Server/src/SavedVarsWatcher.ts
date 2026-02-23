import chokidar, { FSWatcher } from "chokidar"
// import path from "path";
// import * as fs from "fs";
// import { Low } from 'lowdb'

interface OnSavedVarsChanged { (svFilePath: string): Promise<void> }

export default class SavedVarsWatcher {
    svFilePath: string
    watcher?: FSWatcher
    callback: OnSavedVarsChanged


    constructor(svFilePath: string, callback: OnSavedVarsChanged) {
        this.svFilePath = svFilePath
        this.callback = callback
    }

    start() {
        this.watcher = chokidar.watch(this.svFilePath, {
            // ignored: (path, stats) => stats?.isFile() && !path.endsWith('.js'), // only watch js files
            persistent: true,
            awaitWriteFinish: true,
            atomic: true
        })

        this.watcher
            .on("add", async (path) => {
                console.log(`File ${path} has been added`)
                await this.callback(this.svFilePath)
            })
            .on("change", async (path) => {
                console.log(`File ${path} has been changed`)
                await this.callback(this.svFilePath)
            })
    }

    async stop() {
        if (this.watcher) {
            await this.watcher.close()
        }
    }
}