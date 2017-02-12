Rails.application.routes.draw do
  get 'stats/distribution'

  get 'welcome/index'
   
  resources :tanks, :only => [:index, :show]
  get 'tanks_winrate', to: 'tanks#index_winrate', as: 'tankswinrate'
  get 'tanks_wn8', to: 'tanks#index_wn8', as: 'tankswn8'
  
  resources :players, :only => [:index, :show]
  resources :clans, :only => [:index, :show]
  resources :marks, :only => [:index, :show]
  resources :nations, :only => [:index, :show]
  resources :vehicle_types, :only => [:index, :show]

  root 'welcome#index'
  # For details on the DSL available within this file, see http://guides.rubyonrails.org/routing.html
end
