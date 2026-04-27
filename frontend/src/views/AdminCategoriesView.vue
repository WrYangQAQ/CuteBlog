<script setup>
import { onMounted, reactive, ref } from "vue";
import {
  createCategoryApi,
  deleteCategoryApi,
  getCategoriesApi,
  updateCategoryApi
} from "../api/taxonomy";

const rows = ref([]);
const message = ref("");

const form = reactive({
  name: "",
  description: "",
  sortOrder: 1
});

async function loadRows() {
  try {
    const res = await getCategoriesApi();
    rows.value = res.data || [];
  } catch (err) {
    rows.value = [];
    message.value = err?.payload?.message || err.message || "鍔犺浇鍒嗙被澶辫触";
  }
}

async function createRow() {
  try {
    await createCategoryApi({ ...form });
    message.value = "鍒嗙被鍒涘缓鎴愬姛";
    form.name = "";
    form.description = "";
    form.sortOrder = 1;
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍒涘缓鍒嗙被澶辫触";
  }
}

async function updateRow(row) {
  try {
    await updateCategoryApi(row.id, row);
    message.value = "鍒嗙被鏇存柊鎴愬姛";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鏇存柊鍒嗙被澶辫触";
  }
}

async function removeRow(id) {
  if (!confirm("确定删除此分类吗？")) return;
  try {
    await deleteCategoryApi(id);
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "鍒犻櫎鍒嗙被澶辫触";
  }
}

onMounted(loadRows);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>鍒嗙被绠＄悊</h2>
      <div class="inline-form">
        <input v-model.trim="form.name" placeholder="分类名" />
        <input v-model.trim="form.description" placeholder="鎻忚堪" />
        <input v-model.number="form.sortOrder" type="number" min="1" placeholder="鎺掑簭" />
        <button class="btn solid" @click="createRow">鏂板</button>
      </div>
    </div>

    <div class="panel">
      <table class="cute-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>鍚嶇О</th>
            <th>鎻忚堪</th>
            <th>鎺掑簭</th>
            <th>鎿嶄綔</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>{{ row.id }}</td>
            <td><input v-model="row.name" /></td>
            <td><input v-model="row.description" /></td>
            <td><input v-model.number="row.sortOrder" type="number" /></td>
            <td class="table-actions">
              <button class="btn ghost" @click="updateRow(row)">淇濆瓨</button>
              <button class="btn danger" @click="removeRow(row.id)">鍒犻櫎</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
