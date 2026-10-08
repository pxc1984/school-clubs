import { api } from "$lib/api/client";

export type TodoItem = {
    id: string;
    title: string;
    isDone: boolean;
    created: string;
};

const base = "/api/v1/todo-items";

export async function listTodoItems(): Promise<TodoItem[]> {
    const response = await api.get<TodoItem[]>(base);
    return response.data;
}

export async function createTodoItem(title: string): Promise<TodoItem> {
    const response = await api.post<TodoItem>(base, { title });
    return response.data;
}

export async function updateTodoItem(item: TodoItem): Promise<TodoItem> {
    const response = await api.put<TodoItem>(`${base}/${item.id}`, {
        title: item.title,
        isDone: item.isDone,
    });
    return response.data;
}

export async function deleteTodoItem(id: string): Promise<void> {
    await api.delete(`${base}/${id}`);
}
