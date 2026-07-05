import { InventoryTable } from './inventory/InventoryTable';

// This is React's equivalent of Angular's App component + app.html: the root
// that mounts the feature. There's no `imports` array to register
// InventoryTable in, and no template file — importing the function and
// using it as a JSX tag (<InventoryTable />) is the entire wiring step.
// React resolves the tag directly from the import, at compile time; Angular
// resolves app-inventory-table from its selector, at the framework's own
// runtime, which is why Angular needs that explicit imports-array
// registration and React doesn't.
function App() {
  return (
    <>
      <h1>Vin Inventory</h1>
      <InventoryTable />
    </>
  );
}

export default App;
