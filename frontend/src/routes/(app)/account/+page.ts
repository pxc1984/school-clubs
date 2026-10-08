import { getAccount } from "$lib/api/account";
import { listExternalProviders } from "$lib/api/auth";

export async function load({ parent }) {
    const { me } = await parent();
    // у сервисного админа нет аккаунта
    if (me.isServiceAdmin) return { account: null, providers: [] };

    const [account, providers] = await Promise.all([
        getAccount(),
        listExternalProviders().catch(() => []),
    ]);
    return { account, providers };
}
