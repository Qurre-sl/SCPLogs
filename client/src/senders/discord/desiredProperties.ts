const USER_DESIRED_PROPERTIES = {
    username: true,
    discriminator: true,
} as const;

const INTERACTION_DESIRED_PROPERTIES = {
    id: true,
    token: true,
    channelId: true,
    data: true,
    user: true,
} as const;

export function getDesiredProperties() {
    return {
        user: USER_DESIRED_PROPERTIES,
        interaction: INTERACTION_DESIRED_PROPERTIES,
    } as const;
}