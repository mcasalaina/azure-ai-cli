import { FunctionFactory } from './FunctionFactory';
export let factory = new FunctionFactory();

function getCurrentWeather(function_arguments: string): string {
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

factory.addFunction(getCurrentWeatherSchema, getCurrentWeather);

function getCurrentDate(): string {
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

factory.addFunction(getCurrentDateSchema, getCurrentDate);

function getCurrentTime(): string {
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

factory.addFunction(getCurrentTimeSchema, getCurrentTime);

//declare a global array of Any called order
let order: any[] = [];
let total: number = 0;

const menuItems = [
  { name: 'Taco', price: 3.00 },
  { name: 'Burrito', price: 5.00 },
  { name: 'Chimichanga', price: 6.00 },
  { name: 'Quesadilla', price: 4.00 },
  { name: 'Small Drink', price: 1.50 },
  { name: 'Medium Drink', price: 2.00 },
  { name: 'Large Drink', price: 2.50 }
];

function loadMenu(): void {
  const menuItemsDiv = document.getElementById('menu-items');
  if (menuItemsDiv !== null) {
    menuItems.forEach(item => {
      const itemDiv = document.createElement('div');
      itemDiv.className = 'menu-item';
      itemDiv.innerHTML = `${item.name} <button onclick="addItem('${item.name}', ${item.price})">Add - $${item.price.toFixed(2)}</button>`;
      menuItemsDiv.appendChild(itemDiv);
    });
  }
}

window.onload = loadMenu;

function addItem(jsonObject:string): void {
  //Parse the JSON string
  let jsonString = JSON.parse(jsonObject);

  //Extract the name from the JSON string
  let name: string = jsonString.name;
  //Look up the price from the menuItems array
  let price: number | undefined = menuItems.find(item => item.name === name)?.price;
  
  if (price !== undefined) {
    order.push({ name, price });
    total += price;
    updateOrder();
  } else {
    console.error(`Price not found for item: ${name}`);
  }
}

const addItemSchema = {
  name: "add_item",
  description: "Add an item to the order",
  parameters: {
    type: "object",
    properties: {
      name: {
        type: "string",
        description: "The name of the item. Should be one of the items in the menu, which include "
        +
        //Iterate through the menu items and add them to this string comma separated
        menuItems.map(item => item.name).join(",") + " and only those."

      },
    },
    required: ["name", "price"],
  },
};

factory.addFunction(addItemSchema, addItem);

function addMultipleItems(jsonObject: string): void {
  //Parse the JSON string
  let jsonString = JSON.parse(jsonObject);

  //Extract the items array from the JSON string
  let items: any[] = jsonString.items;

  //Iterate through the items array and add each item
  items.forEach((item: any) => {
    addItem(JSON.stringify(item));
  });
}

const addMultipleItemsSchema = {
  name: "add_multiple_items",
  description: "Add multiple items to the order",
  parameters: {
    type: "object",
    properties: {
      items: {
        type: "array",
        description: "An array of items to add to the order",
        items: {
          type: "object",
          properties: {
            name: {
              type: "string",
              description: "The name of the item",
            },
          },
          required: ["name"],
        },
      },
    },
    required: ["items"],
  },
};

factory.addFunction(addMultipleItemsSchema, addMultipleItems);

function removeItem(index:number): void {
  total -= order[index].price;
  order.splice(index, 1);
  updateOrder();
}

const removeItemSchema = {
  name: "remove_item",
  description: "Remove an item from the order",
  parameters: {
    type: "object",
    properties: {
      index: {
        type: "number",
        description: "The index of the item to remove",
      },
    },
    required: ["index"],
  },
};

factory.addFunction(removeItemSchema, removeItem);

function updateOrder(): void {
  const orderItemsDiv = document.getElementById('order-items');
  if (orderItemsDiv === null) throw new Error('Could not find order-items div');
  orderItemsDiv.innerHTML = '';
  order.forEach((item, index) => {
    const itemDiv = document.createElement('div');
    itemDiv.className = 'order-item';
    itemDiv.innerHTML = `${item.name} - $${item.price.toFixed(2)} <button onclick="removeItem(${index})">Remove</button>`;
    orderItemsDiv.appendChild(itemDiv);
  });
  const totalElement = document.getElementById('total');
  if (totalElement === null) {
    throw new Error('Could not find total div');
  } else {
    totalElement.innerText = total.toFixed(2);
  }
}
