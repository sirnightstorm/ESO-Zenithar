import psList from 'ps-list'
import { readFile, writeFile } from "fs/promises"

export default class ProcessedValueWriter {
    svFilePath: string
    watcherInterval?: NodeJS.Timeout = null

    constructor(svFilePath: string) {
        this.svFilePath = svFilePath
    }


    private async isESORunning(): Promise<boolean> {
        const processes = await psList();
        return processes.some(p => p.name.toLowerCase() === "eso64.exe");
    }

    private async markProcessed(filePath: string): Promise<void> {
        // Read file as UTF‑8 text
        let content = await readFile(filePath, "utf8");

        // Replace only the exact match
        const updated = content.replace('["processed"] = 0', '["processed"] = 1');

        // Write back only if something changed
        if (updated !== content) {
            console.log("Marking data as processed")
            await writeFile(filePath, updated, "utf8");
        }
    }

    watchForESOExit(): void {
        console.log("Waiting for ESO to close...");
        this.watcherInterval = setInterval(async () => {
            try {
                const running = await this.isESORunning();

                if (!running) {
                    clearInterval(this.watcherInterval);   // stop checking
                    await this.markProcessed(this.svFilePath);
                    console.log("ESO closed — processed flag updated");
                    this.watcherInterval = null
                }
            } catch (err) {
                console.error("Error checking ESO status:", err);
            }
        }, 10_000); // 10 seconds
    }


    public run() {
        if (this.watcherInterval != null) {
            return // Watcher already running
        }

        this.watchForESOExit()
    }

}