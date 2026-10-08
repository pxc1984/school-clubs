<script lang="ts" module>
    import type { AccessRequestStatus } from "$lib/api/types";

    export const REQUEST_STATUS_LABELS: Record<AccessRequestStatus, string> = {
        Pending: "На рассмотрении",
        Approved: "Одобрен",
        Rejected: "Отклонён",
        Cancelled: "Отозван",
    };
</script>

<script lang="ts">
    import { Badge, type BadgeVariant } from "$lib/components/ui/badge";

    let { status }: { status: AccessRequestStatus } = $props();

    const variants: Record<AccessRequestStatus, BadgeVariant> = {
        Pending: "secondary",
        Approved: "default",
        Rejected: "destructive",
        Cancelled: "outline",
    };
</script>

<Badge variant={variants[status]}>{REQUEST_STATUS_LABELS[status]}</Badge>
