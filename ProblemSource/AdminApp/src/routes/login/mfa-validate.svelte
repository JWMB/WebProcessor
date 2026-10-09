<script lang="ts">
    import { onMount } from 'svelte';
	import { ApiFacade } from "../../apiFacade";
	import { getApi } from "../../globalStore";
	import { LoginUtils } from './LoginUtils';
	import MfaEnable from './mfa-enable.svelte';
	// import { MfaVerifyGetDto } from '../../apiClient';

    const apiFacade = getApi() as ApiFacade;

    export let email: string;
    export let onSuccess: () => void;

    let success: boolean | undefined;
    let mfaSettings: any;

    onMount(async () => {
        mfaSettings = await apiFacade.users.getMfaVerify();
        console.log("mfaSettings", mfaSettings);
    });

    async function submit() {
        let code = (document.getElementById("mfaCode") as HTMLInputElement)?.value || "";
        success = await apiFacade.users.postMfaVerify({ email: email, code: code });
        if (success) {
            onSuccess();
        }
    }

    function checkAutoSubmit(e: KeyboardEvent) {
        if (LoginUtils.checkAutoSubmit(e, mfaSettings?.numTotpDigits))
            submit();
    }

    let cnt = 0;
    let showEnable: boolean;
    function requestReregister() {
        if (cnt++ > 3) { // temp until proper solution
            showEnable = true;
        }
    }

</script>

<div>
    {#if showEnable}
        <MfaEnable email={email} onSuccess={() => onSuccess()}></MfaEnable>

    {:else}
        <form on:submit|preventDefault={submit}>

        {LoginUtils.getText("MfaLogin.enterCode", { appName: mfaSettings?.issuer || "Vektor Teacher"})}
        <div>
            <input name="Code" id="mfaCode" class="form-control"
                    on:keydown={LoginUtils.checkPrevent} 
                    on:keyup={checkAutoSubmit} autofocus autocomplete="off" />
        </div>
        <button type="submit">Submit</button>
        {#if success === false}
        <div style="color: red">Incorrect code, try again</div>
        {/if}
        </form>

        <div>
            <button class="inline-button" on:click={() => requestReregister()}>{LoginUtils.getText("MfaEnable.lostMyApp")}</button>
        </div>
    {/if}

</div>