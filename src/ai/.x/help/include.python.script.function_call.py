import argparse
import importlib
import inspect
import json
import os
import sys
from typing import Generator

def ensure_and_strip_module_path(module_path) -> str:
    module_path = os.path.join(os.getcwd(), module_path)
    module_name = os.path.basename(module_path)

    if os.path.exists(module_path + ".py"):
        module_dirname = os.path.dirname(module_path)
        if module_dirname not in sys.path:
            sys.path.append(module_dirname)
        return module_name
    
    raise ModuleNotFoundError("Module not found: " + module_path)
    
def call_function(module_path: str, function_name: str, json_params: str):
    try:
        module_name = ensure_and_strip_module_path(module_path)

        # Import the module
        target_module = importlib.import_module(module_name)

        # Use inspect to find the function by name
        target_function = getattr(target_module, function_name)

        # Check if the target_function is callable
        if callable(target_function):
            # Parse the JSON parameter string to a dictionary
            params = json.loads(json_params)

            # Get the function's signature
            sig = inspect.signature(target_function)

            # Bind the parameters to the function's signature
            bound_args = sig.bind(**params)

            # Call the function with the bound arguments
            result = target_function(*bound_args.args, **bound_args.kwargs)
            return result

        else:
            return "Function not found or not callable."
        
    # handle TypeError
    except ModuleNotFoundError:
        print("Module not found: " + module_path)
        raise
    except AttributeError:
        print("Function not found: " + function_name)
        raise
    except TypeError:
        print("Invalid JSON parameter(s): " + str(sys.exc_info()[1]))
        raise
    except json.JSONDecodeError:
        print("Invalid JSON parameter: " + json_params)
        raise

def ensure_args() -> list:
    parser = argparse.ArgumentParser(description="Call a function in a module with specified parameters.")
    parser.add_argument("--function", required=True, help="Module and function name in the format MODULE:FUNCTION.")
    parser.add_argument("--parameters", default="{}", help="JSON string containing parameters.")
    args = parser.parse_args()
    
    return args.function, args.parameters

def main():
    function, json_params = ensure_args()
    module_function_parts = function.rsplit(":", 1)
    
    if len(module_function_parts) != 2:
        print("Invalid argument format. Please use MODULE:FUNCTION.")
        sys.exit(1)

    module_name = module_function_parts[0]
    function_name = module_function_parts[1]

    result = call_function(module_name, function_name, json_params)

    if result is not None:
        # if it's a string...
        if isinstance(result, str):
            print("---it's a string---")
            for word in result.split():
                print(word)

        # if it's a list of strings
        elif isinstance(result, list) and all(isinstance(item, str) for item in result):
            print("---it's a list---")
            for item in result:
                for word in item.split():
                    print(word)

        # if it's a generator
        elif issubclass(type(result), Generator) :
            print("---it's a generator---")
            for item in result:
                for word in item.split():
                    print(word)

        else:
            print("---it's something else---")
            print(type(result))

if __name__ == "__main__":
    main()