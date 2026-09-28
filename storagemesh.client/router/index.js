import { createRouter, createWebHistory } from "vue-router";

import Overview from "@/views/OverView.vue";
import Node from "@/views/NodeView.vue";

const router = createRouter({
    history: createWebHistory(),

    routes: [
        {
            path: "/",
            name: "overview",
            component: Overview
        },
        {
            path: "/node/:id",
            name: "node",
            component: Node
        }
    ]
});

export default router;
