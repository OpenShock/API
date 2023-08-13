<template>
  <nav class="navbar first-level">
    <a class="navbar-logo"><img src="@/assets/images/shocklink-logo-white.png" alt="naxoKit Logo"/></a>
    <theme-toggle/>
    <button class="navbar-toggler" @click="mobileShow = !mobileShow" type="button"
            aria-controls="navbarSupportedContent" aria-expanded="false"
            aria-label="Toggle navigation">
      <i class="fas fa-bars text-white"></i>
    </button>
    <div class="collapse navbar-collapse" :class="{show: mobileShow}">
      <ul class="navbar-nav">
        <div class="hori-selector" :style="horiStyle">
          <div class="left"/>
          <div class="right"/>
        </div>
        <item v-for="(item, index) in allElements" :item="item" :index="index" :key="index"/>
        <item-link-external link="https://docs.shocklink.net"><i class="fa-solid fa-book-open-reader"></i>Wiki</item-link-external>
        <item-link-external link="https://github.com/Shock-Link"><i class="fa-brands fa-github"></i>GitHub</item-link-external>
        <item-link v-if="loggedIn" link="/dashboard"><i class="fa-solid fa-right-to-bracket"></i>Dashboard</item-link>
        <item-link v-else link="/account/login"><i class="fa-solid fa-right-to-bracket"></i>Login</item-link>
      </ul>
    </div>
  </nav>
</template>

<script>
import Item from "./Item";
import ThemeToggle from "../../utils/ThemeToggle";
import ItemLink from './ItemLink.vue';
import ItemLinkExternal from './ItemLinkExternal.vue';

export default {
  components: { ThemeToggle, ItemLink, ItemLinkExternal, Item },
  data() {
    return {
      activeItem: "",
      allElements: [
        {
          routerLink: '/public/home',
          html: '<i class="fa-solid fa-house"></i>Home',
          active: false
        }
      ],
      mobileShow: false,
      currentIndex: -1,
      interval: 0,
      hori: {
        top: 0,
        left: 3000,
        width: 150,
        height: 60
      },
      loggedIn: false
    }
  },
  computed: {
    horiStyle() {
      return {
        top: this.hori.top + 'px',
        left: this.hori.left + 'px',
        width: this.hori.width + 'px',
        height: this.hori.height + 'px'
      }
    }
  },
  async beforeMount() {
    this.loggedIn = await utils.checkIfLoggedIn();
  },
  mounted() {
    this.initActive();
    window.addEventListener('resize', () => this.updateHori());
    this.emitter.on('route-after', () => {
      this.initActive();
    });
    setTimeout(() => {
      this.updateHori();
    }, 300)
    this.interval = window.setInterval(() => {
      this.updateHori();
    }, 500);
  },
  unmounted() {
    window.removeEventListener('resize', () => this.updateHori());
    clearInterval(this.interval);
  },
  methods: {
    clicked(index) {
      this.currentIndex = index;
      this.updateActive();
      this.$router.push(this.allElements[this.currentIndex].routerLink);
    },
    initActive() {
      const path = this.$router.currentRoute._value.fullPath
      for (const i in this.allElements) {
        const element = this.allElements[i];
        if (path.includes(element.routerLink)) {
          this.currentIndex = i;
          break;
        }
      }
      this.updateActive();
    },
    updateActive() {
      this.allElements.forEach(ele => ele.active = false);
      const le = this.allElements[this.currentIndex];
      if(le !== undefined) {
        le.active = true;
        this.activeItem = le.that;
      }
      this.updateHori();
    },
    updateHori() {
      if (this.activeItem.$refs === undefined || this.activeItem.$refs.li === undefined) return;
      const element = $(this.activeItem.$refs.li);
      const pos = element.position();
      this.hori.top = pos.top;
      this.hori.left = pos.left;
      this.hori.width = element.innerWidth();
      this.hori.height = element.innerHeight();
    }
  }
}
</script>

<style scoped lang="scss">

.first-level {
  flex: 0 1 auto;
  background-color: var(--nav-bar-color);
  padding: 0;

  .theme-toggle {
    span svg {
      color: #fff;
    }
  }

  .navbar-logo {
    margin: 0 20px 0 10px;

    img {
      height: 48px;
      width: auto;
    }
  }

  .collapse {
    overflow: hidden;
    position: relative;

    ul {
      padding: 0;
      margin: 0 0 0 auto;

      .hori-selector {
        display: inline-block;
        position: absolute;
        height: 100%;
        top: 0;
        left: 0;
        transition-duration: 0.6s;
        transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
        background-color: var(--main-background-color);
        border-top-left-radius: 15px;
        border-top-right-radius: 15px;
        margin-top: 10px;

        .right, .left {
          position: absolute;
          width: 25px;
          height: 25px;
          background-color: var(--main-background-color);
          bottom: 10px;

          &:before {
            content: '';
            position: absolute;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            background-color: var(--nav-bar-color);
            bottom: 0;
          }
        }

        .right {
          right: -25px;

          &:before {
            right: -25px;
          }
        }

        .left {
          left: -25px;

          &:before {
            left: -25px;
          }
        }
      }

      :deep(li) {
        list-style-type: none;
        float: left;
        cursor: pointer;

        &:not(.active) a:hover {
          color: rgba(191, 191, 191, 1);
        }

        a {
          color: #fff;
          text-decoration: none;
          font-size: 15px;
          display: block;
          padding: 20px 20px;
          transition-duration: 0.3s;
          transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
          position: relative;
          user-select: none;

          svg {
            margin-right: 10px;
          }
        }
      }
    }

    & > ul > :deep(li) {
      &.active > a {
        color: var(--main-color);
        background-color: transparent;
        transition: all 0.5s;
        transition-delay: 0.2s;
      }
      &.profile > a {
        padding: 15px 10px 5px 10px;

        img {
          height: 42px;
          border-radius: 50%;
        }
      }
    }
  }
}

@media (min-width: 992px) {
  .first-level {
    flex-flow: row nowrap;
    justify-content: flex-start;

    .navbar-toggler {
      display: none;
    }

    .navbar-collapse {
      display: flex !important;
      flex-basis: auto;

      ul {
        flex-direction: row;
      }
    }
  }
}


@media (max-width: 991px) {
  .first-level {
    .collapse {
      ul {
        :deep(li) a {
          padding: 12px 30px !important;
        }

        .hori-selector {
          margin-top: 0;
          margin-left: 10px;
          border-radius: 25px 0 0 25px;

          .left, .right {
            right: 10px;
          }

          .left {
            top: -25px;
            left: auto;

            &:before {
              left: -25px;
              top: -25px;
            }
          }

          .right {
            bottom: -25px;

            &:before {
              bottom: -25px;
              left: -25px;
            }
          }
        }
      }
    }
  }
}
</style>
