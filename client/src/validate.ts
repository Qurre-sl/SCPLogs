import {z, ZodType} from "zod";

export default function<T extends ZodType>(schema: T, value: unknown) {
    const {success, data, error} = z.safeParse(schema, value);

    if (success) {
        return data;
    }

    throw error;
}