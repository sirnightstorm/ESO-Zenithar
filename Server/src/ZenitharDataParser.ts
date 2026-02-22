// zenitharDataParser.ts
// Strict, non-eval parser for ESO Zenithar_data SavedVariables (Lua subset)

export type LuaScalar = string | number | boolean | null;
export type LuaValue = LuaScalar | LuaTable | LuaArray;

export interface LuaTable {
    [key: string]: LuaValue;
}

export type LuaArray = Array<LuaValue>

class LuaTokenizer {
    private i = 0;

    constructor(private src: string) {}

    peek(): string {
        return this.src[this.i] ?? "";
    }

    next(): string {
        return this.src[this.i++] ?? "";
    }

    skipWhitespaceAndComments() {
        while (this.i < this.src.length) {
            const ch = this.peek();
            if (/\s/.test(ch)) {
                this.next();
                continue;
            }
            // line comments: --
            if (ch === "-" && this.src[this.i + 1] === "-") {
                this.i += 2;
                while (this.i < this.src.length && this.peek() !== "\n") this.i++;
                continue;
            }
            break;
        }
    }

    readString(): string {
        const quote = this.next();
        let out = "";
        while (this.i < this.src.length) {
            const ch = this.next();
            if (ch === quote) break;
            if (ch === "\\") {
                const n = this.next();
                if (n === "n") out += "\n";
                else if (n === "t") out += "\t";
                else out += n;
            } else {
                out += ch;
            }
        }
        return out;
    }

    readNumber(): number {
        const start = this.i;
        if (this.peek() === "-") this.next();
        while (/[0-9]/.test(this.peek())) this.next();
        if (this.peek() === ".") {
            this.next();
            while (/[0-9]/.test(this.peek())) this.next();
        }
        const raw = this.src.slice(start, this.i);
        const n = Number(raw);
        if (Number.isNaN(n)) {
            throw new Error(`Invalid number '${raw}' at ${start}`);
        }
        return n;
    }

    readIdentifier(): string {
        const start = this.i;
        while (/[A-Za-z0-9_@.']/.test(this.peek())) this.next();
        return this.src.slice(start, this.i);
    }
}

class LuaParser {
    private t: LuaTokenizer;

    constructor(src: string) {
        this.t = new LuaTokenizer(src);
    }

    parseTopLevelAssignments(): Record<string, LuaValue> {
        const result: Record<string, LuaValue> = {};
        while (true) {
            this.t.skipWhitespaceAndComments();
            if (!this.t.peek()) break;

            // read identifier (e.g. Zenithar_data)
            const ident = this.t.readIdentifier();
            if (!ident) break;

            this.t.skipWhitespaceAndComments();
            if (this.t.peek() !== "=") {
                throw new Error(`Expected '=' after identifier '${ident}'`);
            }
            this.t.next(); // '='
            this.t.skipWhitespaceAndComments();

            const value = this.parseValue();
            result[ident] = value;

            this.t.skipWhitespaceAndComments();
        }
        return result;
    }

    private parseValue(): LuaValue {
        this.t.skipWhitespaceAndComments();
        const ch = this.t.peek();

        if (ch === "{") return this.parseTableOrArray();
        if (ch === "\"" || ch === "'") return this.t.readString();
        if (ch === "-" || /[0-9]/.test(ch)) return this.t.readNumber();

        return this.parseLiteral();
    }

    private parseLiteral(): LuaScalar {
        const ident = this.t.readIdentifier();
        if (ident === "true") return true;
        if (ident === "false") return false;
        if (ident === "nil") return null;
        throw new Error(`Unexpected literal '${ident}'`);
    }

    private parseTableOrArray(): LuaValue {
        this.t.next(); // '{'
        this.t.skipWhitespaceAndComments();

        const obj: LuaTable = {};
        const arr: LuaArray = [];
        let isArray = true;
        let nextIndex = 1;

        while (true) {
            this.t.skipWhitespaceAndComments();
            const ch = this.t.peek();
            if (ch === "}") {
                this.t.next();
                break;
            }

            let key: string | number | null = null;

            if (ch === "[") {
                // [ "key" ] = or [ 123 ] =
                this.t.next(); // '['
                this.t.skipWhitespaceAndComments();
                const innerCh = this.t.peek();
                let inner: string | number;
                if (innerCh === "\"" || innerCh === "'") {
                    inner = this.t.readString();
                } else if (/[0-9-]/.test(innerCh)) {
                    inner = this.t.readNumber();
                } else {
                    inner = this.t.readIdentifier();
                }
                this.t.skipWhitespaceAndComments();
                if (this.t.peek() !== "]") throw new Error("Expected ']'");
                this.t.next();
                this.t.skipWhitespaceAndComments();
                if (this.t.peek() !== "=") throw new Error("Expected '=' after key");
                this.t.next();
                key = inner;
            } else if (/[A-Za-z_]/.test(ch)) {
                // identifier key: foo = value
                const ident = this.t.readIdentifier();
                this.t.skipWhitespaceAndComments();
                if (this.t.peek() !== "=") {
                    throw new Error("Unexpected bare identifier in table");
                }
                this.t.next();
                key = ident;
            } else {
                // array-style value
                key = null;
            }

            this.t.skipWhitespaceAndComments();
            const value = this.parseValue();

            if (key === null) {
                isArray = isArray && nextIndex === arr.length + 1;
                arr.push(value);
                nextIndex++;
            } else {
                isArray = false;
                obj[String(key)] = value;
            }

            this.t.skipWhitespaceAndComments();
            const sep = this.t.peek();
            if (sep === ",") {
                this.t.next();
                this.t.skipWhitespaceAndComments();
                continue;
            }
            if (sep === "}") {
                this.t.next();
                break;
            }
        }

        return isArray ? arr : obj;
    }
}

/**
 * Parse the Lua file and return the Zenithar_data table as a JS object tree.
 */
export function parseZenitharData(luaText: string): LuaTable {
    const parser = new LuaParser(luaText);
    const assignments = parser.parseTopLevelAssignments();

    if (!assignments["Zenithar_data"]) {
        throw new Error("Zenithar_data not found in Lua file");
    }

    const zen = assignments["Zenithar_data"];
    if (typeof zen !== "object" || zen === null || Array.isArray(zen)) {
        throw new Error("Zenithar_data is not a table");
    }

    return zen as LuaTable;
}
