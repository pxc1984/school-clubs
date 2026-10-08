import { listAccessRequests, listChildren } from "$lib/api/parent";

export async function load() {
    const [children, requests] = await Promise.all([
        listChildren(),
        listAccessRequests(),
    ]);
    return {
        children,
        pending: requests.filter((request) => request.status === "Pending"),
    };
}
