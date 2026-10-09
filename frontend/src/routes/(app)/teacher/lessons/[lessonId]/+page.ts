import { getGradebook, getLesson, listImports } from "$lib/api/teacher";

export async function load({ params }) {
    const [lesson, gradebook, imports] = await Promise.all([
        getLesson(params.lessonId),
        getGradebook(params.lessonId),
        // список файлов — дополнение к журналу, без него страница работает
        listImports(params.lessonId).catch(() => []),
    ]);
    return { lesson, gradebook, imports };
}
