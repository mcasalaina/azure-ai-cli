class MenuItem {
  constructor(public id: string, public name: string, public price: number, public description: string) { }
}

class Menu {
  private items: MenuItem[] = [];

  addItem(item: MenuItem) {
    this.items.push(item);
  }

  getItemById(id: string): MenuItem | undefined {
    return this.items.find(item => item.id === id);
  }

  getItemByName(name: string): MenuItem | undefined {
    return this.items.find(item => item.name === name);
  }

  getItems(): MenuItem[] {
    return this.items;
  }

  loadMenuItemsDiv() {
    const menuItemsDiv = document.getElementById('menu-items');
    if (menuItemsDiv === null) throw new Error('Could not find menu-items div');

    const menuItems = this.getItems();
    if (menuItems.length === 0) {
      menuItemsDiv.innerHTML = 'No items found';
    } else {
      menuItemsDiv.innerHTML = '';
      menuItems.forEach(item => {
        const itemDiv = document.createElement('div');
        itemDiv.className = 'menu-item';
        itemDiv.innerHTML = `${item.name} <button onclick="addItem('${item.name}', 1)">Add - $${item.price.toFixed(2)}</button>`;
        menuItemsDiv.appendChild(itemDiv);
      });
    }
  }
}

class OrderItem {
  constructor(public menuItemId: string, public quantity: number = 1) { }
}

class Order {
  private items: OrderItem[] = [];

  addItemByName(menu: Menu, name: string, quantity: number = 1): void {
    const menuItem = menu.getItemByName(name);
    if (!menuItem) throw new Error(`MenuItem: ${name} not found`);

    const existingItem = this.items.find(item => item.menuItemId === menuItem.id);

    if (existingItem) {
      existingItem.quantity += quantity;
    } else {
      this.items.push(new OrderItem(menuItem.id, quantity));
    }

    this.updateOrderItemsDiv();
  }

  addItemsByName(menu: Menu, orders: { name: string, quantity: number }[]): void {
    for (const order of orders) {
      this.addItemByName(menu, order.name, order.quantity);
    }
  }

  removeItemByName(name: string, quantity: number = 1): void {
    const menuItem = menu.getItemByName(name);
    const item = this.items.find(item => item.menuItemId === menuItem?.id);
    if (!item) throw new Error(`OrderItem: ${name} not found`);

    if (item.quantity > quantity) {
      item.quantity -= quantity;
    } else {
      const index = this.items.indexOf(item);
      this.items.splice(index, 1);
    }

    this.updateOrderItemsDiv();
  }

  removeItemsByName(orders: { name: string, quantity: number }[]): void {
    for (const order of orders) {
      this.removeItemByName(order.name, order.quantity);
    }
  }

  removeItemByIndex(index: number): void {
    if (index >= 0 && index < this.items.length) {
      this.items.splice(index, 1);
    } else {
      throw new Error(`Index ${index} out of bounds`);
    }

    this.updateOrderItemsDiv();
  }

  clearItems(): void {
    this.items = [];
    this.updateOrderItemsDiv();
  }

  getItems(): OrderItem[] {
    return this.items;
  }

  updateOrderItemsDiv() {
    const orderItemsDiv = document.getElementById('order-items');
    if (orderItemsDiv === null) throw new Error('Could not find order-items div');
    orderItemsDiv.innerHTML = '';
    this.items.forEach((item, index) => {
      const menuItem = menu.getItemById(item.menuItemId);
      if (menuItem) {
        const itemDiv = document.createElement('div');
        itemDiv.className = 'order-item';
        itemDiv.innerHTML = `${menuItem.name} - $${(menuItem.price * item.quantity).toFixed(2)} <button onclick="removeItem(${index})">Remove</button>`;
        orderItemsDiv.appendChild(itemDiv);
      }
    });

    const totalElement = document.getElementById('total');
    if (totalElement === null) {
      throw new Error('Could not find total div');
    } else {
      totalElement.innerText = this.getTotalPrice().toFixed(2);
    }
  }

  getTotalPrice(): number {
    return this.items.reduce((acc, item) => {
      const menuItem = menu.getItemById(item.menuItemId);
      return acc + (menuItem ? menuItem.price * item.quantity : 0);
    }, 0);
  }
}

const menu = new Menu();
menu.addItem(new MenuItem("taco", "Taco", 3.00, "A delicious taco"));
menu.addItem(new MenuItem("burrito", "Burrito", 5.00, "A tasty burrito"));
menu.addItem(new MenuItem("chimichanga", "Chimichanga", 6.00, "A spicy chimichanga"));
menu.addItem(new MenuItem("quesadilla", "Quesadilla", 4.00, "A cheesy quesadilla"));
menu.addItem(new MenuItem("small_drink", "Small Drink", 1.50, "A small drink"));
menu.addItem(new MenuItem("medium_drink", "Medium Drink", 2.00, "A medium drink"));
menu.addItem(new MenuItem("large_drink", "Large Drink", 2.50, "A large drink"));
menu.loadMenuItemsDiv();

let order = new Order();
order.updateOrderItemsDiv();

function addItem(name: string, quantity: number) {
  order.addItemByName(menu, name, quantity);
}

function removeItem(index: number) {
  order.removeItemByIndex(index);
}

(window as any).addItem = addItem;
(window as any).removeItem = removeItem;

// -------------------- Custom Functions --------------------

import { FunctionFactory } from './FunctionFactory';
export let factory = new FunctionFactory();

// -------------------- get_current_weather --------------------
function get_current_weather(function_arguments: string): string {
  const location = JSON.parse(function_arguments).location;
  return `The weather in ${location} is 72 degrees and sunny.`;
};

const getCurrentWeatherSchema = {
  name: "get_current_weather",
  description: "Get the current weather in a given location",
  parameters: {
    type: "object",
    properties: {
      location: {
        type: "string",
        description: "The city and state, e.g. San Francisco, CA",
      },
      unit: {
        type: "string",
        enum: ["celsius", "fahrenheit"],
      },
    },
    required: ["location"],
  },
};

factory.addFunction(getCurrentWeatherSchema, get_current_weather);

// -------------------- get_current_date --------------------
function get_current_date(): string {
  const date = new Date();
  return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
}

const getCurrentDateSchema = {
  name: "get_current_date",
  description: "Get the current date",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getCurrentDateSchema, get_current_date);

// -------------------- get_current_time --------------------
function get_current_time(): string {
  const date = new Date();
  return `${date.getHours()}:${date.getMinutes()}:${date.getSeconds()}`;
}

const getCurrentTimeSchema = {
  name: "get_current_time",
  description: "Get the current time",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getCurrentTimeSchema, get_current_time);

// -------------------- get_menu_items --------------------
function get_menu_items(): string {
  return JSON.stringify(menu.getItems());
}

const getMenuItemsSchema = {
  name: "get_menu_items",
  description: "Get the menu items (id, name, price, description)",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getMenuItemsSchema, get_menu_items);

// -------------------- add_menu_item_to_order --------------------
function add_menu_item_to_order(function_arguments: string): string {

  const { id, quantity = 1 } = JSON.parse(function_arguments);

  const item = menu.getItemById(id);
  if (!item) {
    return `Item with id ${id} not found`;
  }

  order.addItemByName(menu, item.name, quantity);

  return `Added ${item.name} to order`;
}

const addMenuItemToOrderSchema = {
  name: "add_menu_item_to_order",
  description: "Add a menu item to the order",
  parameters: {
    type: "object",
    properties: {
      id: {
        type: "string",
        description: "The id of the menu item",
      },
      quantity: {
        type: "number",
        description: "The quantity of the menu item",
      },
    },
    required: ["id"],
  },
};

factory.addFunction(addMenuItemToOrderSchema, add_menu_item_to_order);

// -------------------- remove_menu_item_from_order --------------------
function remove_menu_item_from_order(function_arguments: string): string {
  const { id, quantity = 1 } = JSON.parse(function_arguments);

  const item = menu.getItemById(id);
  if (!item) {
    return `Item with id ${id} not found`;
  }

  order.removeItemByName(item.name, quantity);

  return `Removed ${item.name} from order`;
}

const removeMenuItemFromOrderSchema = {
  name: "remove_menu_item_from_order",
  description: "Remove a menu item from the order",
  parameters: {
    type: "object",
    properties: {
      id: {
        type: "string",
        description: "The id of the menu item",
      },
      quantity: {
        type: "number",
        description: "The quantity of the menu item",
      },
    },
    required: ["id"],
  },
};

factory.addFunction(removeMenuItemFromOrderSchema, remove_menu_item_from_order);

// -------------------- clear_order --------------------
function clearOrder(): string {
  order.clearItems();
  return 'Order cleared';
}

const clearOrderSchema = {
  name: "clear_order",
  description: "Clear the order",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(clearOrderSchema, clearOrder);

// -------------------- get_order_total --------------------
function getOrderTotal(): string {
  return order.getTotalPrice().toFixed(2);
}

const getOrderTotalSchema = {
  name: "get_order_total",
  description: "Get the total price of the order",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getOrderTotalSchema, getOrderTotal);

// -------------------- get_order_items --------------------
function getOrderItems(): string {
  return JSON.stringify(order.getItems());
}

const getOrderItemsSchema = {
  name: "get_order_items",
  description: "Get the items in the order",
  parameters: {
    type: "object",
    properties: {},
  },
};

factory.addFunction(getOrderItemsSchema, getOrderItems);