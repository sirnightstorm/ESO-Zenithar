import express from "express";
import { create } from "express-handlebars";
import path from "path";
import DB from "./DB"

export default class WebServer {
    app = express()
    port = 8001

    constructor() {
        this.app.set("root", path.resolve(path.join(__dirname, "..")));

        const hbs = create({
            // Specify helpers which are only registered on this instance.
            helpers: {
                toLocaleString(num: number) {  return num.toLocaleString() },
            }
        });

        this.app.engine('handlebars', hbs.engine);
        this.app.set('view engine', 'handlebars');
        this.app.set('views', path.join(__dirname, "..", "views"))

        this.app.use(express.static(__dirname + "/../node_modules/bootstrap/dist"))

        this.app.use((err, req, res, next) => {
            console.error("Express error:", err);
            res.status(500).send("Internal Server Error");
        });

        this.setRoutes()
    }

    public listen() {
        this.app.listen(this.port, () => {
            return console.log(`Express is listening at http://localhost:${this.port}`);
         });
    }

    private setRoutes() {
        this.app.get('/', (req, res) => {
            res.render('home', {
                title: "Home",
            });
        });

        this.app.get('/donations', async (req, res) => {
            res.render('donations', {
                title: "Donations",
                data: await this.getDonationsData()
            });
        });
    }

    private async getDonationsData(): Promise<object> {
        const db = await DB.open()
        const data = await db.getTopDonations()
        await db.close()

        return data
    }

}