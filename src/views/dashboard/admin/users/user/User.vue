<template>
  <div class="col-4 base-widget">
    <div class="row navi-wrap">
      <div class="col row navi">
        <div class="col-4" @click="view = 'read-only'">
          <p :class="{ active: view === 'read-only' }">Read</p>
        </div>
        <div class="col-4" @click="view = 'edit'">
          <p :class="{ active: view === 'edit' }">Edit</p>
        </div>
        <div class="col-4" @click="view = 'new'">
          <p :class="{ active: view === 'new' }">New</p>
        </div>
      </div>
      <transition name="component-fade" mode="out-in">
        <component class="asdf" :is="view" :requestedUser="requestedUser"></component>
      </transition>
    </div>
  </div>
</template>

<script>
import ReadOnly from "./ReadOnly";
import Edit from "./Edit";
import New from "./New";

export default {
  components: {ReadOnly, Edit, New},
  data() {
    return {
      view: 'read-only',
      requestedUser: ''
    }
  },
  mounted() {
    this.emitter.on('getUser', userId => {
      this.view = 'read-only';
      this.requestedUser = userId;
    });
    this.emitter.on('editUser', userId => {
      this.view = 'edit';
      this.requestedUser = userId;
    });
  },
}
</script>

<style scoped lang="scss">
:deep(.scuff) {
  flex: 0 0 auto;
  width: 39px;
}

:deep(.input-uid) {
  margin-bottom: 15px;
}

:deep(.form-user input) {
  margin-bottom: 5px;
}

.asdf {
  height: calc(100% - 50px)
}

.navi-wrap {
  padding-top: 0 !important;
  height: 100%;

  .navi {
    border-bottom: var(--third-background-color) solid 1px;
    margin: 0 0 20px;
    padding: 0;
    height: 50px;

    .col-4 {
      cursor: pointer;
      height: 50px;
      transition: background-color 0.1s ease-in-out;
      flex: 1 1 auto;

      p {
        text-align: center;
        line-height: 50px;
        transition: color 0.1s ease-in-out;

        &.active {
          color: var(--main-color);
        }
      }

      &:hover {
        background: var(--third-background-color);
      }

      & + .col-4 {

        &:before {
          transform: translateY(20%);
          width: 2px;
          height: 70%;
          float: left;
          background-color: var(--main-seperator-color);
          margin-left: -13px;
          content: '';
        }
      }
    }
  }
}
</style>
