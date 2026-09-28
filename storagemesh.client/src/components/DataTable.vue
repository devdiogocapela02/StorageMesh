<script setup>
import { computed, ref, watch } from "vue";

const props = defineProps({
  title: {
    type: String,
    default: "",
  },
  columns: {
    type: Array,
    required: true,
  },
  rows: {
    type: Array,
    default: () => [],
  },
  pageSize: {
    type: Number,
    default: 5,
  },
});

const safeRows = computed(() => (Array.isArray(props.rows) ? props.rows : []));

const currentPage = ref(1);

const totalPages = computed(() => Math.max(1, Math.ceil(safeRows.value.length / props.pageSize)));

const paginatedRows = computed(() => {
  const start = (currentPage.value - 1) * props.pageSize;

  return safeRows.value.slice(start, start + props.pageSize);
});

function previousPage() {
  if (currentPage.value > 1) currentPage.value--;
}

function nextPage() {
  if (currentPage.value < totalPages.value) currentPage.value++;
}

watch(
  () => safeRows,
  () => {
    if (currentPage.value > totalPages.value) currentPage.value = totalPages.value;
  },
);
</script>

<template>
  <section class="data-table">
    <table>
      <caption v-if="title">
        {{
          title
        }}
      </caption>
      <thead>
        <tr>
          <th v-for="column in columns" :key="column.key">
            {{ column.label }}
          </th>
          <th v-if="$slots.actions">Actions</th>
        </tr>
      </thead>

      <tbody>
        <tr v-if="paginatedRows.length === 0">
          <td :colspan="columns.length">No data.</td>
        </tr>

        <tr v-for="(row, index) in paginatedRows" :key="row.id ?? row.fileKey ?? index">
          <td v-for="column in columns" :key="column.key">
            {{ column.format ? column.format(row[column.key]) : row[column.key] }}
          </td>
          <td v-if="$slots.actions">
            <slot name="actions" :row="row" />
          </td>
        </tr>
      </tbody>
    </table>

    <div v-if="safeRows.length > pageSize" class="pagination">
      <button class="btn" :disabled="currentPage === 1" @click="previousPage">
        <span>Previous</span>
      </button>

      <span class="pagination-caption"> Page {{ currentPage }} / {{ totalPages }} </span>

      <button class="btn" :disabled="currentPage === totalPages" @click="nextPage">
        <span>Next</span>
      </button>
    </div>
  </section>
</template>
