<script setup>
import { onMounted } from "vue";
import { network } from "@/network";
import "@/assets/main.css";
import "@/assets/variables.css";
import "@/assets/tables.css";

async function loadNetwork() {
  const response = await fetch("/api/network");
  if (response.ok) network.value = await response.json();
}

onMounted(() => {
  loadNetwork();
  setInterval(loadNetwork, 30000);
});
</script>

<template>
  <header class="title-section">
    <h2>StorageMesh</h2>
  </header>
  <nav class="tabs">
    <RouterLink class="navigation" to="/"
      ><span class="navigation-content">Overview</span></RouterLink
    >
    <RouterLink
      class="navigation"
      v-for="node in network"
      :key="node.id"
      :to="{
        name: 'node',
        params: { id: node.id },
      }"
    >
      <span class="navigation-content">
        <span class="status"></span>
        <span>{{ node.id }}</span>
      </span></RouterLink
    >
  </nav>
  <main class="fill main">
    <RouterView class="fill" />
  </main>
  <footer>
    <span>Freelance_quote</span>
    <a href="https://github.com/devdiogocapela02/FreelanceQuote" target="_blank"
      >View more on Github</a
    >
    <span>2026</span>
  </footer>
</template>
