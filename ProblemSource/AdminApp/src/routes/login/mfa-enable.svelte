<script lang="ts">
    import QRCodeStyling from "qr-code-styling";
    import { onMount } from 'svelte';
	import { ApiFacade } from "../../apiFacade";
	import { getApi } from "../../globalStore";
	import { LoginUtils } from "./LoginUtils";

    const apiFacade = getApi() as ApiFacade;

    export let email: string;
    export let onSuccess: () => void;

    let registedSuccessfully: boolean | undefined;
    let mfaInfo: any;
    
    // Create a reference variable to hold the DOM element
    let qrContainer: HTMLDivElement;

    onMount(async () => {
        const mfaEnableInfo = await apiFacade.users.getMfaEnable(email);
        mfaInfo = mfaEnableInfo;
        console.log("mfaEnableInfo", mfaEnableInfo);

        const qrCode = new QRCodeStyling({
        width: 300,
        height: 300,
        type: "svg", // Can be "canvas" or "svg"
        data: mfaEnableInfo.authenticatorUri,
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

    function checkAutoSubmit(e: KeyboardEvent) {
        // TODO:, mfaSettings?.numTotpDigits
        if (LoginUtils.checkAutoSubmit(e))
            submit();
    }

    function submit() {
        let code = "";
        if (!code) {
            const v = (document.getElementById("mfaCode") as HTMLInputElement)?.value
            if (!v) {
                console.error("no value");
            }
            code = v;
        }
        apiFacade.users.postMfaEnable({ email: email, code: code }) //    returnUrl?: string | undefined
            .then(success => {
                registedSuccessfully = success;
                if (success) {
                    onSuccess();
                }
            });
    }

</script>

<div>
    <p>
        1. {LoginUtils. getText("MfaEnable.downloadApp")}
            <a href="https://go.microsoft.com/fwlink/?Linkid=825072" target="_blank" rel="noopener">Android</a> / 
            <a href="https://go.microsoft.com/fwlink/?Linkid=825073" target="_blank" rel="noopener">iOS</a>
    </p>
    <p>
        2. {LoginUtils.getText("MfaEnable.scanCode")}
    </p>
    <div bind:this={qrContainer}></div>
    <div id="canvas"></div>
    <div id="qrCode" style="margin: 0 auto;width: 200px;"></div>
    <div id="qrCodeData" data-url="@Model.AuthenticatorUri"></div>

    <p>
        3. {LoginUtils.getText("MfaEnable.receiveVerificationCode", { numDigits: mfaInfo?.numTotpDigits })}
    </p>
    <form on:submit|preventDefault={submit}>

        <label class="control-label">4. {LoginUtils.getText("MfaEnable.enterVerificationCode")}</label>
        <input name="Code" id="mfaCode" class="form-control"
            on:keydown={LoginUtils.checkPrevent} 
            on:keyup={checkAutoSubmit} autofocus autocomplete="off" />
        {#if registedSuccessfully === false}
        <div style="color: red">Incorrect code, try again</div>
        {/if}
        <button type="submit">Submit</button>
        <!-- <input type="button" id="submit" value="Submit" on:click={e => submit()}/> -->
    </form>
</div>