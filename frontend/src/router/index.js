import { createRouter, createWebHistory } from "vue-router";
import HomeView from "../views/HomeView.vue";
import ArticlesView from "../views/ArticlesView.vue";
import LoginView from "../views/LoginView.vue";
import RegisterView from "../views/RegisterView.vue";
import ArticleDetailView from "../views/ArticleDetailView.vue";
import ProfileView from "../views/ProfileView.vue";
import PublishArticleView from "../views/PublishArticleView.vue";
import EditArticleView from "../views/EditArticleView.vue";
import AdminDashboardView from "../views/AdminDashboardView.vue";
import AdminCategoriesView from "../views/AdminCategoriesView.vue";
import AdminTagsView from "../views/AdminTagsView.vue";
import CategoriesView from "../views/CategoriesView.vue";
import TagsView from "../views/TagsView.vue";
import ArchiveView from "../views/ArchiveView.vue";
import MessageBoardView from "../views/MessageBoardView.vue";

const routes = [
  { path: "/login", name: "login", component: LoginView, meta: { guestOnly: true } },
  { path: "/register", name: "register", component: RegisterView, meta: { guestOnly: true } },
  { path: "/", name: "home", component: HomeView, meta: { requiresAuth: true } },
  { path: "/articles", name: "articles", component: ArticlesView, meta: { requiresAuth: true } },
  { path: "/categories", name: "categories", component: CategoriesView, meta: { requiresAuth: true } },
  { path: "/tags", name: "tags", component: TagsView, meta: { requiresAuth: true } },
  { path: "/archive", name: "archive", component: ArchiveView, meta: { requiresAuth: true } },
  { path: "/messages", name: "messages", component: MessageBoardView, meta: { requiresAuth: true } },
  {
    path: "/articles/:id",
    name: "article-detail",
    component: ArticleDetailView,
    meta: { requiresAuth: true }
  },
  { path: "/profile", name: "profile", component: ProfileView, meta: { requiresAuth: true } },
  {
    path: "/publish",
    name: "publish",
    component: PublishArticleView,
    meta: { requiresAuth: true }
  },
  {
    path: "/articles/:id/edit",
    name: "edit-article",
    component: EditArticleView,
    meta: { requiresAuth: true }
  },
  {
    path: "/admin/dashboard",
    name: "admin-dashboard",
    component: AdminDashboardView,
    meta: { requiresAuth: true, requiresAdmin: true }
  },
  {
    path: "/admin/categories",
    name: "admin-categories",
    component: AdminCategoriesView,
    meta: { requiresAuth: true, requiresAdmin: true }
  },
  {
    path: "/admin/tags",
    name: "admin-tags",
    component: AdminTagsView,
    meta: { requiresAuth: true, requiresAdmin: true }
  },
  { path: "/:pathMatch(.*)*", redirect: "/" }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach((to, _from, next) => {
  const token = localStorage.getItem("token");
  const role = localStorage.getItem("role");

  if (to.meta.requiresAuth && !token) {
    next("/login");
    return;
  }

  if (to.meta.requiresAdmin && role !== "Admin") {
    next("/");
    return;
  }

  if (to.meta.guestOnly && token) {
    next("/");
    return;
  }

  next();
});

export default router;
