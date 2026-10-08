import type { DateOnly, TimeOnly } from "$lib/api/types";

/** У /auth/me части имени могут быть null */
type NameLike = {
    firstName?: string | null;
    middleName?: string | null;
    lastName?: string | null;
};

export function fullName(person: NameLike): string {
    return [person.lastName, person.firstName, person.middleName]
        .filter(Boolean)
        .join(" ");
}

/** «Иванов Пётр» — для таблиц и сетки */
export function shortName(person: NameLike): string {
    return [person.lastName, person.firstName].filter(Boolean).join(" ");
}

export function initials(person: NameLike): string {
    return [person.firstName, person.lastName]
        .map((part) => part?.charAt(0) ?? "")
        .join("")
        .toUpperCase();
}

/** DateOnly разбираем как локальную дату, иначе в UTC− сдвинется день. */
export function parseDate(value: DateOnly): Date {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
}

export function toDateOnly(date: Date): DateOnly {
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${date.getFullYear()}-${month}-${day}`;
}

export function today(): DateOnly {
    return toDateOnly(new Date());
}

const dateFormat = new Intl.DateTimeFormat("ru-RU", {
    day: "numeric",
    month: "long",
});
const dateWithYearFormat = new Intl.DateTimeFormat("ru-RU", {
    day: "numeric",
    month: "long",
    year: "numeric",
});
const shortDateFormat = new Intl.DateTimeFormat("ru-RU", {
    day: "2-digit",
    month: "2-digit",
});
const weekdayFormat = new Intl.DateTimeFormat("ru-RU", { weekday: "short" });
const dateTimeFormat = new Intl.DateTimeFormat("ru-RU", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
});

/** «12 сентября», с годом — только если не текущий. */
export function formatDate(value: DateOnly): string {
    const date = parseDate(value);
    return date.getFullYear() === new Date().getFullYear()
        ? dateFormat.format(date)
        : dateWithYearFormat.format(date);
}

/** «12.09» — для осей и заголовков колонок */
export function formatShortDate(value: DateOnly): string {
    return shortDateFormat.format(parseDate(value));
}

export function formatWeekday(value: DateOnly): string {
    return weekdayFormat.format(parseDate(value));
}

export function formatDateTime(value: string): string {
    return dateTimeFormat.format(new Date(value));
}

/** «18:30» из «18:30:00» */
export function formatTime(value: TimeOnly | null): string {
    return value ? value.slice(0, 5) : "";
}

const decimalFormat = new Intl.NumberFormat("ru-RU", {
    maximumFractionDigits: 1,
});

/** «3,2» */
export function formatDecimal(value: number): string {
    return decimalFormat.format(value);
}

export function formatPercent(fraction: number): string {
    return `${Math.round(fraction * 100)}%`;
}

const pluralRules = new Intl.PluralRules("ru-RU");

/** plural(5, ["задача", "задачи", "задач"]) → «задач» */
export function plural(
    count: number,
    [one, few, many]: [string, string, string],
): string {
    switch (pluralRules.select(count)) {
        case "one":
            return one;
        case "few":
            return few;
        default:
            return many;
    }
}

export function countOf(
    count: number,
    forms: [string, string, string],
): string {
    return `${count} ${plural(count, forms)}`;
}
