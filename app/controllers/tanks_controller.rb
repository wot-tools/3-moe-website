class TanksController < ApplicationController

	def index
		@tanks = Tank.all
	end
	
	def show
		@tank = Tank.find(params[:id])
		rescue ActiveRecord::RecordNotFound
			redirect_to tanks_path and return
	end
end
