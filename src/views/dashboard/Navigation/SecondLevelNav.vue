<template>
  <nav class="navbar second-level-navbar">
    <div class="navbar-collapse">
      <ul class="navbar-nav">
        <transition-group tag="div" name="component-fade">
          <second-level-item v-for="(item, index) in $store.state.secondLevelNav" :item="item" :key="item.routerLink"/>
        </transition-group>
      </ul>
    </div>
  </nav>
</template>

<script>
import SecondLevelItem from "./SecondLevelItem";
export default {
  components: {SecondLevelItem}
}
</script>

<style scoped lang="scss">

.second-level-navbar {
  height: 45px;
  background-color: var(--secondary-background-color);
  transition: background-color 0.5s;

  flex-flow: row nowrap;
  justify-content: flex-start;

  .navbar-collapse {
    overflow: hidden;
    position: relative;
    display: flex !important;
    flex-basis: auto;
    height: 62.5px;

    ul {
      padding: 0;
      margin: 0 auto;
      flex-direction: row;

      :deep(li) {
        list-style-type: none;
        float: left;
        cursor: pointer;

        a {
          color: var(--main-color);
          text-decoration: none;
          font-size: 15px;
          display: block;
          padding: 20px 20px;
          transition-duration: 0.3s;
          transition-timing-function: cubic-bezier(0.68, -0.55, 0.265, 1.55);
          position: relative;
          user-select: none;

          &:hover {
            color: var(--main-lighten-color);
          }

          svg {
            margin-right: 10px;
          }
        }
      }
    }
  }
}

</style>
