import { createRouter, createWebHistory } from "vue-router";
import HomeView from "../views/HomeView.vue";
import LoginView from "../views/LoginView.vue";
import RegisterView from "../views/RegisterView.vue";
import ArticleDetailView from "../views/ArticleDetailView.vue";
import ProfileView from "../views/ProfileView.vue";
import MyArticlesView from "../views/MyArticlesView.vue";
import PublishArticleView from "../views/PublishArticleView.vue";
import EditArticleView from "../views/EditArticleView.vue";
import AdminDashboardView from "../views/AdminDashboardView.vue";
import AdminCategoriesView from "../views/AdminCategoriesView.vue";
import AdminTagsView from "../views/AdminTagsView.vue";

const routes = [
  { path: "/login", name: "login", component: LoginView, meta: { guestOnly: true } },
  { path: "/register", name: "register", component: RegisterView, meta: { guestOnly: true } },
  { path: "/", name: "home", component: HomeView, meta: { requiresAuth: true } },
  {
    path: "/articles/:id",
    name: "article-detail",
    component: ArticleDetailView,
    meta: { requiresAuth: true }
  },
  { path: "/profile", name: "profile", component: ProfileView, meta: { requiresAuth: true } },
  {
    path: "/my-articles",
    name: "my-articles",
    component: MyArticlesView,
    meta: { requiresAuth: true }
  },
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
