<script lang="ts">
    import QRCodeStyling from "qr-code-styling";
    import { onMount } from 'svelte';

    const NumTotpDigits = 6;

    const getText = (id: string) => {
        const strs: any = {
            "MfaEnable.downloadApp": "Ladda ner eller öppna en app för tvåfaktorsautenticering såsom Microsoft Authenticator:",
            "MfaEnable.scanCode": "Skanna QR-koden med autenticerings-appen.",
            "MfaEnable.receiveVerificationCode": `Din autenticeringsapp ger dig en ${NumTotpDigits}-siffrig verifieringskod`,
            "MfaEnable.enterVerificationCode": "Skriv in verifieringskoden här:"
        }
        return strs[id] || "N/A";
    };


    // Create a reference variable to hold the DOM element
    let qrContainer: HTMLDivElement;

    onMount(async () => {
        const qrCode = new QRCodeStyling({
        width: 300,
        height: 300,
        type: "svg", // Can be "canvas" or "svg"
        data: "https://svelte.dev",
        dotsOptions: {
            color: "#000000",
            type: "square"   // Options: rounded, dots, classy, classy-rounded, square, extra-rounded
        },
        backgroundOptions: {
            color: "#ffffff"
        },
        imageOptions: {
            crossOrigin: "anonymous",
            margin: 10
        }
        });

        if (qrContainer) {
            qrCode.append(qrContainer);
        }
    });

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

    function checkPrevent(e: KeyboardEvent) {
        // console.log(keyCode, e);
        if (isNav(e) || isNumeric(e) || isCtrlCmd(e)) { }
        else e.preventDefault();
    }
    function checkAutoSubmit(e: KeyboardEvent) {
        if (isNav(e)) return;
        if (isCtrlCmd(e) && e.keyCode != 86) return; //skip checks when ctrl+? except when ? = "v"
        const el = e.target as HTMLInputElement;
        if (el?.value.length == NumTotpDigits) {
            document.getElementsByTagName("form")[0].submit();
        }
    }
</script>

<div>
    <p>
        1. {getText("MfaEnable.downloadApp")}
            <a href="https://go.microsoft.com/fwlink/?Linkid=825072" target="_blank" rel="noopener">Android</a> / 
            <a href="https://go.microsoft.com/fwlink/?Linkid=825073" target="_blank" rel="noopener">iOS</a>
    </p>
    <p>
        2. {getText("MfaEnable.scanCode")}
    </p>
    <div bind:this={qrContainer}></div>
    <div id="canvas"></div>
    <div id="qrCode" style="margin: 0 auto;width: 200px;"></div>
    <div id="qrCodeData" data-url="@Model.AuthenticatorUri"></div>

    <p>
        3. {getText("MfaEnable.receiveVerificationCode")}
    </p>
        <div class="form-group">
            <label class="control-label">4. {getText("MfaEnable.enterVerificationCode")}</label>
            <input name="Code" class="form-control" on:keydown={e => checkPrevent(e)} on:keyup={e => checkAutoSubmit(e)} autofocus autocomplete="off" />
            <input name="SecretKey" hidden value="@Model.SecretKey"/>
            <input name="AuthenticatorUri" hidden value="@Model.AuthenticatorUri" />
            <input name="Email" hidden value="@Model.Email" />
            <input name="NumTotpDigits" hidden value="@Model.NumTotpDigits" />
            <input name="AppName" hidden value="@Model.AppName" />
            <input name="ReturnUrl" hidden value="@Model.ReturnUrl" />
        </div>
</div>