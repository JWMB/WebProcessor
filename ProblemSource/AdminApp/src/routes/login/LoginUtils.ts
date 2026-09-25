    function isCtrlCmd(e: KeyboardEvent) {
        if (e.keyCode == 17) return true;
        if (e.ctrlKey || e.metaKey) return true;
        return false;
    }
    function isNav(e: KeyboardEvent) {
        const keyCode = e.keyCode;
        if ([8, 46].indexOf(keyCode) >= 0) return true; // backspace, del
        if ([33, 34, 35, 36, 37, 39, 38, 40].indexOf(keyCode) >= 0) return true; // pgup/pgdn/end/home/left/right/up/down
        return false;
    }
    function isNumeric(e: KeyboardEvent) { return e.keyCode >= 48 && e.keyCode <= 57; }

export class LoginUtils {
    static numTotpDigits = 6;

    static getText(id: string, replacements?: any) {
        const getReplacement = (key: string) => (replacements ? replacements[key] : null) || "N/A";
        const strs: any = {
            "MfaEnable.downloadApp": "Ladda ner eller öppna en app för tvåfaktorsautenticering såsom Microsoft Authenticator:",
            "MfaEnable.scanCode": "Skanna QR-koden med autenticerings-appen.",
            "MfaEnable.receiveVerificationCode": `Din autenticeringsapp ger dig en ${getReplacement("numDigits") || LoginUtils.numTotpDigits}-siffrig verifieringskod`,
            "MfaEnable.enterVerificationCode": "Skriv in verifieringskoden här:",
            "MfaLogin.enterCode": `Öppna den app du använder för tvåfaktorsautenticering, hämta koden för '${getReplacement("appName")}' och skriv in nedan.`,
            "MfaEnable.lostMyApp": "Jag har inte tillgång till min ursprungliga app, kan ni fixa så jag kan registrera igen?"
        }
        return strs[id] || "N/A";
    }
   
    static checkPrevent(e: KeyboardEvent) {
        if (isNav(e) || isNumeric(e) || isCtrlCmd(e)) { }
        else e.preventDefault();
    }
    static checkAutoSubmit(e: KeyboardEvent, numDigits?: number) {
        if (isNav(e)) return false;
        if (isCtrlCmd(e) && e.keyCode != 86) return false; //skip checks when ctrl+? except when ? = "v"
        const el = e.target as HTMLInputElement;
        return el?.value.length == (numDigits || LoginUtils.numTotpDigits);
    }
}