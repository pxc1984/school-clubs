import { listAdminRequests } from "$lib/api/admin";
import type { AccessRequestStatus } from "$lib/api/types";

const STATUSES: AccessRequestStatus[] = [
    "Pending",
    "Approved",
    "Rejected",
    "Cancelled",
];

export async function load({ url }) {
    const raw = url.searchParams.get("status");
    // по умолчанию — очередь на рассмотрение; «all» — все
    const status =
        raw === "all"
            ? null
            : STATUSES.includes(raw as AccessRequestStatus)
              ? (raw as AccessRequestStatus)
              : "Pending";
    const selected = url.searchParams.get("request");
    return { status, selected, requests: await listAdminRequests(status) };
}
