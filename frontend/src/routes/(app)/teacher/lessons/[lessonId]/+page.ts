import { getGradebook, getLesson } from "$lib/api/teacher";

export async function load({ params }) {
    const [lesson, gradebook] = await Promise.all([
        getLesson(params.lessonId),
        getGradebook(params.lessonId),
    ]);
    return { lesson, gradebook };
}
