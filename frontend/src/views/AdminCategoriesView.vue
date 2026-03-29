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
    message.value = err?.payload?.message || err.message || "加载分类失败";
  }
}

async function createRow() {
  try {
    await createCategoryApi({ ...form });
    message.value = "分类创建成功";
    form.name = "";
    form.description = "";
    form.sortOrder = 1;
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "创建分类失败";
  }
}

async function updateRow(row) {
  try {
    await updateCategoryApi(row.id, row);
    message.value = "分类更新成功";
  } catch (err) {
    message.value = err?.payload?.message || err.message || "更新分类失败";
  }
}

async function removeRow(id) {
  if (!confirm("确定删除此分类吗？")) return;
  try {
    await deleteCategoryApi(id);
    await loadRows();
  } catch (err) {
    message.value = err?.payload?.message || err.message || "删除分类失败";
  }
}

onMounted(loadRows);
</script>

<template>
  <section class="stack">
    <div class="panel">
      <h2>分类管理</h2>
      <p v-if="message" :class="message.includes('成功') ? 'ok' : 'error'">{{ message }}</p>
      <div class="inline-form">
        <input v-model.trim="form.name" placeholder="分类名" />
        <input v-model.trim="form.description" placeholder="描述" />
        <input v-model.number="form.sortOrder" type="number" min="1" placeholder="排序" />
        <button class="btn solid" @click="createRow">新增</button>
      </div>
    </div>

    <div class="panel">
      <table class="cute-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>名称</th>
            <th>描述</th>
            <th>排序</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>{{ row.id }}</td>
            <td><input v-model="row.name" /></td>
            <td><input v-model="row.description" /></td>
            <td><input v-model.number="row.sortOrder" type="number" /></td>
            <td class="table-actions">
              <button class="btn ghost" @click="updateRow(row)">保存</button>
              <button class="btn danger" @click="removeRow(row.id)">删除</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
