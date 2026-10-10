<script lang="ts">
    import type { ChartPointDto } from "$lib/api/parent";
    import { formatDate, formatPercent, formatShortDate } from "$lib/format";

    let { series }: { series: ChartPointDto[] } = $props();

    const HEIGHT = 220;
    const PAD = { top: 12, right: 12, bottom: 28, left: 36 };
    const TICKS = [0, 0.25, 0.5, 0.75, 1];

    let width = $state(600);
    let hovered = $state<number | null>(null);

    const innerWidth = $derived(Math.max(width - PAD.left - PAD.right, 1));
    const innerHeight = HEIGHT - PAD.top - PAD.bottom;

    function x(index: number): number {
        if (series.length === 1) return PAD.left + innerWidth / 2;
        return PAD.left + (index / (series.length - 1)) * innerWidth;
    }

    function y(value: number): number {
        return PAD.top + (1 - value) * innerHeight;
    }

    /** Ломаная по точкам, где значение есть; пропуски рвут линию. */
    function path(values: (number | null)[]): string {
        let d = "";
        let pen = false;
        values.forEach((value, index) => {
            if (value === null) {
                pen = false;
                return;
            }
            d += `${pen ? "L" : "M"}${x(index).toFixed(1)},${y(value).toFixed(1)}`;
            pen = true;
        });
        return d;
    }

    /** Полоса 25–75 перцентиль: верхняя граница вперёд, нижняя назад. */
    const band = $derived.by(() => {
        const points = series
            .map((point, index) => ({ ...point, index }))
            .filter(
                (point) => point.groupP25 !== null && point.groupP75 !== null,
            );
        if (points.length < 2) return "";
        const upper = points.map((p) => `${x(p.index)},${y(p.groupP75!)}`);
        const lower = points
            .toReversed()
            .map((p) => `${x(p.index)},${y(p.groupP25!)}`);
        return `M${upper.join("L")}L${lower.join("L")}Z`;
    });

    const childPath = $derived(path(series.map((point) => point.score)));
    const medianPath = $derived(path(series.map((point) => point.groupMedian)));
    const hasBand = $derived(band !== "");
    const hasMedian = $derived(
        series.some((point) => point.groupMedian !== null),
    );

    // подписи оси X прореживаем, чтобы не налезали
    const labelStep = $derived(
        Math.max(1, Math.ceil(series.length / Math.max(innerWidth / 56, 1))),
    );

    function onPointerMove(event: PointerEvent) {
        const svg = event.currentTarget as SVGSVGElement;
        const left = svg.getBoundingClientRect().left;
        const relative = (event.clientX - left - PAD.left) / innerWidth;
        const index = Math.round(relative * (series.length - 1));
        hovered = Math.min(Math.max(index, 0), series.length - 1);
    }

    const active = $derived(hovered !== null ? series[hovered] : null);
</script>

<figure class="flex flex-col gap-3">
    <div class="relative" bind:clientWidth={width}>
        <svg
            {width}
            height={HEIGHT}
            class="block touch-pan-y overflow-visible select-none"
            role="img"
            aria-label="Доля решённых задач по занятиям"
            onpointermove={onPointerMove}
            onpointerleave={() => (hovered = null)}
        >
            {#each TICKS as tick (tick)}
                <line
                    x1={PAD.left}
                    x2={PAD.left + innerWidth}
                    y1={y(tick)}
                    y2={y(tick)}
                    class="stroke-border"
                    stroke-dasharray={tick === 0 ? undefined : "2 4"}
                />
                <text
                    x={PAD.left - 6}
                    y={y(tick)}
                    dy="0.32em"
                    text-anchor="end"
                    class="fill-muted-foreground text-[10px]"
                >
                    {formatPercent(tick)}
                </text>
            {/each}

            {#each series as point, index (point.lessonId)}
                {#if index % labelStep === 0}
                    <text
                        x={x(index)}
                        y={HEIGHT - 8}
                        text-anchor="middle"
                        class="fill-muted-foreground text-[10px]"
                    >
                        {formatShortDate(point.date)}
                    </text>
                {/if}
            {/each}

            {#if hasBand}
                <path d={band} class="fill-muted-foreground/15" />
            {/if}
            {#if hasMedian}
                <path
                    d={medianPath}
                    fill="none"
                    stroke-width="1.5"
                    stroke-dasharray="4 3"
                    class="stroke-muted-foreground"
                />
            {/if}
            <path
                d={childPath}
                fill="none"
                stroke-width="2.5"
                stroke-linejoin="round"
                class="stroke-primary"
            />

            {#each series as point, index (point.lessonId)}
                {#if point.score !== null}
                    <circle
                        cx={x(index)}
                        cy={y(point.score)}
                        r={hovered === index ? 5 : 3.5}
                        class="fill-primary stroke-background"
                        stroke-width="2"
                    />
                {:else}
                    <!-- пропуск: отметка на оси, а не ноль -->
                    <circle
                        cx={x(index)}
                        cy={y(0)}
                        r="3"
                        class="fill-background stroke-muted-foreground"
                        stroke-width="1.5"
                    />
                {/if}
            {/each}

            {#if hovered !== null}
                <line
                    x1={x(hovered)}
                    x2={x(hovered)}
                    y1={PAD.top}
                    y2={PAD.top + innerHeight}
                    class="stroke-foreground/30"
                />
            {/if}
        </svg>

        {#if active && hovered !== null}
            <div
                class="pointer-events-none absolute top-0 z-10 w-max rounded-md border bg-popover px-2.5 py-1.5 text-xs shadow-md"
                style:left="{Math.min(
                    Math.max(x(hovered) - 70, 0),
                    width - 150,
                )}px"
            >
                <div class="font-medium">{formatDate(active.date)}</div>
                <div>
                    Ребёнок:
                    {active.score === null
                        ? "пропуск"
                        : formatPercent(active.score)}
                </div>
                {#if active.groupMedian !== null}
                    <div class="text-muted-foreground">
                        Медиана группы: {formatPercent(active.groupMedian)}
                    </div>
                {/if}
            </div>
        {/if}

        <table class="sr-only" aria-label="Доля решённых задач по занятиям">
            <thead>
                <tr><th>Дата</th><th>Ребёнок</th><th>Медиана группы</th></tr>
            </thead>
            <tbody>
                {#each series as point (point.lessonId)}
                    <tr>
                        <td>{formatDate(point.date)}</td>
                        <td>
                            {point.score === null
                                ? "пропуск"
                                : formatPercent(point.score)}
                        </td>
                        <td>
                            {point.groupMedian === null
                                ? "—"
                                : formatPercent(point.groupMedian)}
                        </td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>

    <figcaption
        class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground"
    >
        <span class="flex items-center gap-1.5">
            <span class="h-0.5 w-4 rounded bg-primary"></span>Ваш ребёнок
        </span>
        {#if hasMedian}
            <span class="flex items-center gap-1.5">
                <span
                    class="w-4 border-t-2 border-dashed border-muted-foreground"
                ></span>Медиана группы
            </span>
        {/if}
        {#if hasBand}
            <span class="flex items-center gap-1.5">
                <span class="h-3 w-4 rounded-sm bg-muted-foreground/15"
                ></span>Половина группы (25–75%)
            </span>
        {/if}
        <span class="flex items-center gap-1.5">
            <span
                class="size-2 rounded-full border-[1.5px] border-muted-foreground"
            ></span>Пропуск
        </span>
    </figcaption>
</figure>
